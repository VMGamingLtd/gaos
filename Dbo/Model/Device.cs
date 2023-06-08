﻿namespace gaos.Dbo.Model
{
    /*
    public enum PlatformType
    {
        WebGL,
        PC,
        Android,
        IOS
    }
    */


    public class Device
    {
        public int Id;
        public string? Identification;

        public string? PlatformType;

        public int? BuildVersionId;
        public BuildVersion? BuildVersion;

        public string? BuildVersionReported;

        public System.DateTime RegisteredAt;
    }
}
