
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Linq;
using System.IO;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JHAllowedIDCreation
{
    class Program
    {
        static ConcurrentDictionary<string, int> groupCounts = new ConcurrentDictionary<string, int>();

        static Config config;
        static string[] exclusionPatientID = [];
        static string[] inclusionPatientID = [];

        private static Config? cheakConfigFile()
        {
            // 実行ファイルと同じDirのconfig.jsonの存在確認
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (!File.Exists(configFilePath))
            {

                //config.jsonが存在しない場合
                // config.jsonを作成する
                Console.WriteLine("config.jsonが存在しません。config.jsonを作成します。");
                string configJson =
@"{
    ""SSMIX2"": {
        ""dataPath"": ""D:\\SSMIX2\\data"",
        ""getChikenID"": false,
        ""StartDate"": ""2015-04-01"",
        ""exclusionDays"":365        
    },
    ""MNGCP"":{
        ""db_host"": ""localhost"",
        ""db_port"": 5432,
        ""db_name"": ""new_odbcdb"",
        ""db_user"": ""postgres"",
        ""db_password"": ""your_password"",
        ""db_table_name"": ""v_hiken_info"",
        ""db_encoding"": ""UTF8""
    }
}";
                File.WriteAllText(configFilePath, configJson);
                Console.WriteLine("config.jsonを作成しました。");
                Console.WriteLine("config.jsonを編集して、DBの接続情報を設定してください。");
                Console.WriteLine("Press any key to exit...");
                return null;
            }
            else
            {
                //config.jsonが存在する場合
                // config.jsonを読み込み、Configクラスのインスタンスを作成する
                string json = File.ReadAllText(configFilePath);
                Config config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(json);
                if (config == null)
                {
                    Console.WriteLine("config.jsonの読み込みに失敗しました。");
                    Console.WriteLine("Press any key to exit...");

                    return null;
                }
                else
                {
                    // config.jsonの内容を表示する
                    Console.WriteLine("config.jsonの内容を表示します。");
                    Console.WriteLine(json);
                    return config;
                }
            }
        }
        static async Task Main(string[] args)
        {
            exclusionPatientID = [];
            config = cheakConfigFile();
            if (config == null)
            {
                Console.ReadKey();
                return;
            }


            // configのSSMIX2のdataPathが存在するか確認
            if (!Directory.Exists(config.SSMIX2.dataPath))
            {
                Console.WriteLine("SSMIX2のdataPathが存在しません。");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // configのSSMIX2のgetChikenIDがtrueの場合
            // 除外する患者IDを取得する
            if (!config.SSMIX2.getChikenID)
            {
                // ファイル選択ダイアログでcsvファイルを選択
                // 患者IDが含まれるcsvファイル
                string filePath = "";
                using (var dialog = new CommonOpenFileDialog())
                {
                    dialog.IsFolderPicker = false;
                    dialog.Filters.Add(new CommonFileDialogFilter("CSV Files", "*.csv"));
                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        filePath = dialog.FileName;
                        // filePathのデータを読み込む
                        Console.WriteLine("選択されたファイル: " + filePath);
                        // csvファイルを読み込む
                        // ヘッダー無し、カンマ区切り、Shift_JIS
                        // すべての行を読み込む
                        var TempExclusionPatientID = File.ReadAllLines(filePath, Encoding.GetEncoding("UTF-8"));
                        // TempExclusionPatientIDを10桁左0埋めの半角英数字に変換してexclusionPatientIDに追加
                        exclusionPatientID = TempExclusionPatientID.Select(x => x.Trim().PadLeft(10, '0')).ToArray();
                    }
                    else
                    {
                        Console.WriteLine("除外患者ファイルが選択されませんでした。");
                    }
                }
                // configのMNGCPのDB接続できるか確認
                string connectionString = $"Host={config.MNGCP.db_host};Port={config.MNGCP.db_port};Username={config.MNGCP.db_user};Password={config.MNGCP.db_password};Database={config.MNGCP.db_name};Encoding={config.MNGCP.db_encoding}";
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        Console.WriteLine("NMGCP接続成功");

                        // NMGCPから患者IDを取得
                        var query = "SELECT * FROM " + config.MNGCP.db_table_name;
                        using (var cmd = new NpgsqlCommand(query, connection))
                        using (var reader = cmd.ExecuteReader())
                        {
                            // データ行出力
                            while (reader.Read())
                            {
                                var value = reader.IsDBNull(1) ? "" : reader.GetValue(1).ToString();
                                if (value != null)
                                {
                                    // 患者IDを取得
                                    // exclusionPatientIDに追加
                                    exclusionPatientID = exclusionPatientID.Append(value).ToArray();
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("NMGCP接続失敗");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                        return;
                    }
                }
            }
            startDate = config.SSMIX2.StartDate == null ? DateTime.MinValue : DateTime.Parse(config.SSMIX2.StartDate);
            cutDate = DateTime.Now.AddDays(-config.SSMIX2.exclusionDays);

            // SSMIX2フォルダの解析スタート
            Console.WriteLine("SSMIX2フォルダの解析スタート...");
            await SSMIX2FolderParse();


        }
        static DateTime startDate;
        static DateTime cutDate;
        // SSMIX2フォルダの解析
        private static async Task SSMIX2FolderParse()
        {
            List<Task> tasks = new List<Task>();
            // 先頭3文字フォルダ（例: "000", "001", ...）を列挙
            var topFolders = Directory.GetDirectories(config.SSMIX2.dataPath);
            foreach (var topFolder in topFolders)
            {
                // 中位フォルダ（患者ID 4～6文字）の列挙
                var midFolders = Directory.GetDirectories(topFolder);
                foreach (var midFolder in midFolders)
                {
                    // 中位フォルダ単位でタスクを生成し、並列実行
                    tasks.Add(Task.Run(() => ProcessMidFolder(midFolder)));
                }
            }

            await Task.WhenAll(tasks);

            // 集計結果を CSV へ出力
            OutputToCsv();

            Console.WriteLine("全処理が完了しました。");
        }
        static long completedPatientCount = 0;
        static void ProcessMidFolder(string midFolder)
        {
            var patientFolders = Directory.GetDirectories(midFolder);
            Parallel.ForEach(patientFolders,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 16 },
                patientFolder =>
                {
                    // 患者IDはフォルダ名
                    string patientId = Path.GetFileName(patientFolder);
                    long count = Interlocked.Increment(ref completedPatientCount);
                    if (!exclusionPatientID.Contains(patientId))
                    {
                        if (!checkDirDate(patientFolder))
                        {
                            ProcessPatientFolder(patientFolder);
                            // inclusionPatientIDにpatientId追加
                            inclusionPatientID = inclusionPatientID.Append(patientId).ToArray();

                        }
                    }
                    if (count % 500 == 0)
                    {
                        Console.WriteLine($"{count} 患者フォルダの処理が完了しました。内提供対象:{inclusionPatientID.Length}件");
                    }
                });
        }
        static bool checkDirDate(string dirPath)
        {
            var visitDateFolders = Directory.GetDirectories(dirPath).OrderByDescending(name => name); 

            // visitDateFoldersをlinqで解析し、cutDateより新しい日付が含まれているかチェックする
            // 日付はvisitDateFolders[i]の末尾に含まれる
            // 例: "20230101" のような形式
            // 末尾の文字列を取得し、DateTimeに変換
            // 変換できたら、cutDateより新しいかチェック
            // 一つでも新しい日付があれば、trueを返す
            // 変換できなかったら、スキップ
            bool keizou = true;
            foreach (var visitDateFolder in visitDateFolders)
            {
                var lastText = visitDateFolder.Split(Path.DirectorySeparatorChar).Last();
                if (DateTime.TryParseExact(lastText, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime visitDate))
                {
                    // 診療日が除外日よりも新しい場合はスキップ
                    if (visitDate > cutDate)
                    {
                        return true;
                    }
                    if(visitDate > startDate)
                    {
                        keizou = false;
                    }
                }
            }
            return keizou;
        }
        static void ProcessPatientFolder(string patientFolder)
        {
            try
            {
                string patientId = Path.GetFileName(patientFolder);
                var fileItems = new List<(string filePath, string visitDate, string folderDataType)>();

                // 診療日フォルダ（例：YYYYMMDD）が並ぶ
                var visitDateFolders = Directory.GetDirectories(patientFolder).OrderByDescending(name => name); 
        
                foreach (var visitDateFolder in visitDateFolders)
                {
                    string visitDate = Path.GetFileName(visitDateFolder);
                    // データ種別フォルダ（例: ADT-01, PPR-01 等）
                    var dataTypeFolders = Directory.GetDirectories(visitDateFolder);
                    foreach (var dataTypeFolder in dataTypeFolders)
                    {
                        string folderDataType = Path.GetFileName(dataTypeFolder);
                        // ファイル名の末尾が "_1" のみを対象
                        var files = Directory.GetFiles(dataTypeFolder, "*_1");
                        foreach (var file in files)
                        {
                            fileItems.Add((file, visitDate, folderDataType));
                        }
                    }
                }

                // 各ファイルを並列に処理（高い並列度：CPUコア数の4倍程度）
                Parallel.ForEach(fileItems,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 16 },
                    item =>
                    {
                        ProcessFile(item.filePath, patientId, item.visitDate, item.folderDataType);
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[警告] 患者フォルダ {patientFolder} の処理中にエラー: {ex.Message}");
            }
        }
        static void ProcessFile(string filePath, string patientId, string visitDate, string folderDataType)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            try
            {
                // ファイル名（拡張子なし）を取得
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                var parts = fileName.Split('_');
                if (parts.Length != 7)
                {
                    return;
                }

                // ファイル名に含まれるデータ種別（例: PPR-01）
                string dataType = parts[2];

                if (dataType.Equals("PPR-01", StringComparison.OrdinalIgnoreCase))
                {
                    // PPR-01 はファイル内容を ISO-2022-JP で読み込み、PRB セグメントから日付を取得
                    //                    Encoding iso2022jp = Encoding.GetEncoding("ISO-2022-JP");
                    string[] lines;
                    try
                    {
                        lines = File.ReadAllLines(filePath, Encoding.GetEncoding("ISO-2022-JP"));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[警告] ファイル読み込み失敗 (PPR-01): {filePath}, {e.Message}");
                        return;
                    }

                    // groupCounts は ConcurrentDictionary<string, int> である前提
                    Parallel.ForEach(lines,
                     new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 32 },
                    line =>
                    {
                        if (!line.StartsWith("PRB|")) return;

                        var seg = line.Split('|');
                        if (seg.Length <= 7) return;

                        string dateTimeStr = seg[7]; // 例: 20220420153210
                        if (dateTimeStr.Length < 8) return;

                        string dateStr = dateTimeStr.Substring(0, 8);
                        string key = $"{patientId},{dateStr},PPR-01";

                        groupCounts.AddOrUpdate(key, 1, (_, current) => current + 1);
                    });

                }
                else
                {
                    // PPR-01 以外は、フォルダ構造上の診療日 (visitDate) を利用して1件カウント
                    string key = $"{patientId},{visitDate},{dataType}";
                    groupCounts.AddOrUpdate(key, 1, (k, current) => current + 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[警告] ファイル処理エラー: {filePath}, {ex.Message}");
            }
        }
        ConcurrentDictionary<string, ConcurrentDictionary<string, int>> pivotData = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();

        static void OutputToCsv()
        {
            try
            {
                // user download folder
                string outputCsvFileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                string datetime = DateTime.Now.ToString("yyyyMMddHHmmss");
                string filename = "result" + datetime + "_Syuukei.csv";
                string filename3 = "result" + datetime + "_pivot.csv";
                string filename2 = "result" + datetime + "_PatientIDs.csv";
                // inclusionPatientID
                string newOutputCsvFile = Path.Combine(outputCsvFileDir, filename);
                using (var sw = new StreamWriter(newOutputCsvFile, false, Encoding.UTF8))
                {
                    sw.WriteLine("患者ID,診療日,データ種別,Count");
                    foreach (var kvp in groupCounts)
                    {
                        sw.WriteLine($"{kvp.Key},{kvp.Value}");
                    }
                }
                newOutputCsvFile = Path.Combine(outputCsvFileDir, filename3);
                using (var sw = new StreamWriter(newOutputCsvFile, false, Encoding.UTF8))
                {
                    var gpDate = groupCounts.GroupBy(x => x.Key.Split(',')[1]).Select(x => x);
                    var gpTypes = groupCounts.GroupBy(x => x.Key.Split(',')[2]).Select(x => x);
                    sw.Write("Date,");
                    foreach (var type in gpTypes)
                    {
                        sw.Write($"{type.Key},");
                    }
                    sw.WriteLine();
                    foreach (var date in gpDate)
                    {
                        // date.Keyは日付(20100903など)
                        // 日付に変換をtryparseして、DateTimeに変換
                        if (!DateTime.TryParseExact(date.Key, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                        {
                            sw.Write($",");
                        }
                        else
                        {
                            // 日付をyyyy/MM/dd形式に変換
                            sw.Write($"{dateTime.ToString("yyyy/MM/dd")},");
                        }
                        foreach (var type in gpTypes)
                        {
                            var count = date.FirstOrDefault(x => x.Key.Split(',')[2] == type.Key).Value;
                            sw.Write($"{count},");
                        }
                        sw.WriteLine();
                    }
                }
                newOutputCsvFile = Path.Combine(outputCsvFileDir, filename2);
                using (var sw = new StreamWriter(newOutputCsvFile, false, Encoding.UTF8))
                {
                    sw.WriteLine("出力対象患者ID");
                    foreach (var id in inclusionPatientID)
                    {
                        sw.WriteLine($"{id}");
                    }
                }

                Console.WriteLine($"CSV 出力完了: {newOutputCsvFile}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CSV 出力エラー: {ex.Message}");
            }
        }
    }
}