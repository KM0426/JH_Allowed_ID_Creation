# 使用方法
1. dist.zipをダウンロード、解凍
2. 解凍したフォルダ内の「JH_Allowed_ID_Creation.exe」を初回実行
3. 初回実行時にconfig.jsonが作成されるので、config.jsonに必要情報を記載
   config.json{
        "SSMIX2": {
            "dataPath": "SSMIX2 標準ストレージのdataフォルダを指定",
            "getChikenID": false or true, #false = 治験IDを除外
            "StartDate": "2015-04-01",
            "exclusionDays":365        
        },
        "MNGCP":{
            "db_host": "localhost",
            "db_port": 5432,
            "db_name": "new_odbcdb",
            "db_user": "postgres",
            "db_password": "your_password",
            "db_table_name": "chiken_table_name",
            "db_encoding": "UTF8"
        }
    }
5. 　
