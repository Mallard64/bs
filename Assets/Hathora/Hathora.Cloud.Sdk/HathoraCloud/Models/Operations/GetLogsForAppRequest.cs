
//------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by Speakeasy (https://speakeasy.com). DO NOT EDIT.
//
// Changes to this file may cause incorrect behavior and will be lost when
// the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#nullable enable
namespace HathoraCloud.Models.Operations
{
    using HathoraCloud.Models.Shared;
    using HathoraCloud.Utils;
    using System;
    using UnityEngine;
    
    [Serializable]
    public class GetLogsForAppRequest
    {

        [SerializeField]
        [SpeakeasyMetadata("pathParam:style=simple,explode=false,name=appId")]
        public string? AppId { get; set; }

        [SerializeField]
        [SpeakeasyMetadata("queryParam:style=form,explode=true,name=follow")]
        public bool? Follow { get; set; }

        [SerializeField]
        [SpeakeasyMetadata("queryParam:style=form,explode=true,name=region")]
        public Region? Region { get; set; }

        [SerializeField]
        [SpeakeasyMetadata("queryParam:style=form,explode=true,name=tailLines")]
        public int? TailLines { get; set; }
    }
}