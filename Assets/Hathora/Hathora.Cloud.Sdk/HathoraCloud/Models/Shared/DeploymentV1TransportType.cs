
//------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by Speakeasy (https://speakeasy.com). DO NOT EDIT.
//
// Changes to this file may cause incorrect behavior and will be lost when
// the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#nullable enable
namespace HathoraCloud.Models.Shared
{
    using Newtonsoft.Json;
    using System;
    using UnityEngine;
    [Obsolete("This enum will be removed in a future release, please migrate away from it as soon as possible")]
    public enum DeploymentV1TransportType
    {
        [JsonProperty("tcp")]
        Tcp,
        [JsonProperty("udp")]
        Udp,
        [JsonProperty("tls")]
        Tls,
    }

#pragma warning disable 0618
    public static class DeploymentV1TransportTypeExtension
    {
        public static string Value(this DeploymentV1TransportType value)
        {
            return ((JsonPropertyAttribute)value.GetType().GetMember(value.ToString())[0].GetCustomAttributes(typeof(JsonPropertyAttribute), false)[0]).PropertyName ?? value.ToString();
        }

        public static DeploymentV1TransportType ToEnum(this string value)
        {
            foreach(var field in typeof(DeploymentV1TransportType).GetFields())
            {
                var attributes = field.GetCustomAttributes(typeof(JsonPropertyAttribute), false);
                if (attributes.Length == 0)
                {
                    continue;
                }

                var attribute = attributes[0] as JsonPropertyAttribute;
                if (attribute != null && attribute.PropertyName == value)
                {
                    return (DeploymentV1TransportType)field.GetValue(null);
                }
            }

            throw new Exception($"Unknown value {value} for enum DeploymentV1TransportType");
        }
    }
#pragma warning restore 0618

}