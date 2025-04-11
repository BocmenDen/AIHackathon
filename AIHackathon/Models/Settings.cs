﻿namespace AIHackathon.Models
{
    public class Settings
    {
        public int MaxCountMetricsCommand { get; set; } = 100;
        public required string LinkNewsGroup { get; set; }
        public int CountCommandsInPage { get; set; } = 10;
        public required string[] ValidTypesHandleMediaFile { get; set; }
        public string PathNormalizeFiles { get; set; } = "./NormalizeFiles";
        public string PathUserFiles { get; set; } = "./UserFiles";
        public string PathRoot { get; set; } = "./";
        public int WaitUpdateMessageTestingModel { get; set; } = 1000;
    }
}
