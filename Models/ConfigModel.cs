namespace JHAllowedIDCreation
{
    class SSMIX2
    {
        public string dataPath { get; set; }
        public bool getChikenID { get; set; }
        public int exclusionDays { get; set; }
        public string StartDate { get; set; }
    }
    class NMGCP
    {
        public string db_host { get; set; }
        public int db_port { get; set; }
        public string db_name { get; set; }
        public string db_user { get; set; }
        public string db_password { get; set; }
        public string db_table_name { get; set; }
        public string db_encoding { get; set; }
    }
    class Config
    {
        public SSMIX2 SSMIX2 { get; set; }
        public NMGCP NMGCP { get; set; }
    }
}