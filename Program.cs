
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
        static Config config;
        static string[] exclusionPatientID;

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
        ""getChikenID"": false
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
        static void Main(string[] args)
        {
            Config? config = cheakConfigFile();
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
                        exclusionPatientID = File.ReadAllLines(filePath, Encoding.GetEncoding("UTF-8"));
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
                        var query = "SELECT * FROM " + config.MNGCP.db_table_name;
                        using (var cmd = new NpgsqlCommand(query, connection))
                        using (var reader = cmd.ExecuteReader())
                        {
                            // データ行出力
                            while (reader.Read())
                            {
                                var value = reader.IsDBNull(2) ? "" : reader.GetValue(2).ToString();
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


            // SSMIX2フォルダの解析スタート

            // NM<GCPから患者IDを取得

        }
    }
}