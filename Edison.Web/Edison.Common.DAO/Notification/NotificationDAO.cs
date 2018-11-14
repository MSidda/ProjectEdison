﻿using Edison.Core.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Edison.Common.Interfaces;

namespace Edison.Common.DAO
{
    public class NotificationDAO : IEntityDAO
    {
        public string Title { get; set; }
        public string User { get; set; }
        public string ResponseId { get; set; } 
        public string NotificationText { get; set; }
        public List<string> Tags { get; set; }
        public int Status { get; set; }
        public string Id { get; set; }
        [JsonConverter(typeof(EpochDateTimeConverter))]
        public DateTime CreationDate { get; set; }
        [JsonConverter(typeof(EpochDateTimeConverter))]
        public DateTime UpdateDate { get; set; }
        public string ETag { get; set; }
    }
}
