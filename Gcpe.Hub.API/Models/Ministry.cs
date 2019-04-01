﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gcpe.Hub.API.Models
{
    public class Ministry
    {
        public System.Guid Id { get; set; }
        public string Key { get; set; }
        public int SortOrder { get; set; }
        public string DisplayName { get; set; }
        public string Abbreviation { get; set; }
        public bool IsActive { get; set; }
        public string MinisterEmail { get; set; }
        public string MinisterPhotoUrl { get; set; }
        public string MinisterPageHtml { get; set; }
        public System.DateTime Timestamp { get; set; }
        public string MiscHtml { get; set; }
        public string MiscRightHtml { get; set; }
        public string TwitterUsername { get; set; }
        public string FlickrUrl { get; set; }
        public string YoutubeUrl { get; set; }
        public string AudioUrl { get; set; }
        public string FacebookEmbedHtml { get; set; }
        public string YoutubeEmbedHtml { get; set; }
        public string AudioEmbedHtml { get; set; }
        public Nullable<System.Guid> TopReleaseId { get; set; }
        public Nullable<System.Guid> FeatureReleaseId { get; set; }
        public string MinisterAddress { get; set; }
        public string MinisterName { get; set; }
        public string MinisterSummary { get; set; }
        public Nullable<System.Guid> ParentId { get; set; }
        public string MinistryUrl { get; set; }
        public Nullable<int> ContactUserId { get; set; }
        public Nullable<int> SecondContactUserId { get; set; }
        public string WeekendContactNumber { get; set; }
        public Nullable<System.DateTimeOffset> EodFinalizedDateTime { get; set; }
        public Nullable<int> EodLastRunUserId { get; set; }
        public Nullable<System.DateTimeOffset> EodLastRunDateTime { get; set; }
        public string DisplayAdditionalName { get; set; }
    
    }
}
