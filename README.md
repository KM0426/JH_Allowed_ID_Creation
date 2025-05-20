# 使用方法
1. dist.zipをダウンロード、解凍
2. 解凍したフォルダ内の「JH_Allowed_ID_Creation.exe」を初回実行
3. 初回実行時にconfig.jsonが作成されるので、config.jsonに必要情報を記載
   config.json\{
        "SSMIX2": \{
            "dataPath": "SSMIX2 標準ストレージのdataフォルダを指定",
            "getChikenID": false or true, # false = 治験IDを除外
            "StartDate": "2015-04-01", # 受診対象期間の頭
            "exclusionDays":365 # 実行日から指定日数前までの期間に受診歴がある患者を除外する
        \},
        "NMGCP":\{ # NMGCPのViewテーブル情報
            "db_host": "localhost",
            "db_port": 5432,
            "db_name": "new_odbcdb",
            "db_user": "postgres",
            "db_password": "your_password",
            "db_table_name": "chiken_table_name",
            "db_encoding": "UTF8"
        \}
    \}
5. 再度実行し完了まで放置（数時間かかります）
6. 3種類のresultファイルが出力されます
　　PatientIDs、Syuukei(集計用)、pivot(集計用)
7. PatientIDsに抽出対象IDが出力されます

# 対象外IDファイル
1. 解析実行時のファイル選択で除外するIDがある場合、csvファイルを取り込むことで事前に除外されます(MCDRsの抽出後の解析結果から作成し、追加しておくと便利)
　(例)除外.csv
　　0000000001
　　0000000002
　　0000000003
　　0000000004
    ...