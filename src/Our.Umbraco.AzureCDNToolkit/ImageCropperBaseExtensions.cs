﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

using Umbraco.Core.Composing;
using Umbraco.Core.PropertyEditors.ValueConverters;

namespace Our.Umbraco.AzureCDNToolkit
{
    internal static class ImageCropperBaseExtensions
    {

        internal static ImageCropperValue.ImageCropperCrop GetImageCrop(this string json, string id)
        {
            var ic = new ImageCropperValue.ImageCropperCrop();
            if (json.DetectIsJson())
            {
                try
                {
                    var imageCropperSettings = JsonConvert.DeserializeObject<List<ImageCropperValue.ImageCropperCrop>>(json);
                    ic = imageCropperSettings.GetCrop(id);
                }
                catch (Exception ex)
                {
                    Current.Logger.Error(typeof(ImageCropperBaseExtensions), ex, "Could not parse the json string: {json}", json);
                }
            }

            return ic;
        }

        internal static ImageCropperValue SerializeToCropDataSet(this string json)
        {
            var imageCrops = new ImageCropperValue();
            if (json.DetectIsJson())
            {
                try
                {
                    imageCrops = JsonConvert.DeserializeObject<ImageCropperValue>(json);
                }
                catch (Exception ex)
                {
                    Current.Logger.Error(typeof(ImageCropperBaseExtensions), ex, "Could not parse the json string: {json}", json);
                }
            }

            return imageCrops;
        }

        internal static ImageCropperValue.ImageCropperCrop GetCrop(this ImageCropperValue dataSet, string cropAlias)
        {
            if (dataSet == null || dataSet.Crops == null || !dataSet.Crops.Any())
                return null;

            return dataSet.Crops.GetCrop(cropAlias);
        }

        internal static ImageCropperValue.ImageCropperCrop GetCrop(this IEnumerable<ImageCropperValue.ImageCropperCrop> dataSet, string cropAlias)
        {

            if (dataSet == null)
                return null;

            var imageCropperCrops = dataSet as ImageCropperValue.ImageCropperCrop[] ?? dataSet.ToArray();

            if (!imageCropperCrops.Any())
                return null;

            return string.IsNullOrEmpty(cropAlias)
                ? imageCropperCrops.FirstOrDefault()
                : imageCropperCrops.FirstOrDefault(x => string.Equals(x.Alias, cropAlias, StringComparison.InvariantCultureIgnoreCase));
        }

        internal static string GetCropBaseUrl(this ImageCropperValue cropDataSet, string cropAlias, bool preferFocalPoint)
        {
            var cropUrl = new StringBuilder();

            var crop = cropDataSet.GetCrop(cropAlias);

            // if crop alias has been specified but not found in the Json we should return null
            if (string.IsNullOrEmpty(cropAlias) == false && crop == null)
            {
                return null;
            }
            if ((preferFocalPoint && cropDataSet.HasFocalPoint()) || (crop != null && crop.Coordinates == null && cropDataSet.HasFocalPoint()) || (string.IsNullOrEmpty(cropAlias) && cropDataSet.HasFocalPoint()))
            {
                cropUrl.Append("?center=" + cropDataSet.FocalPoint.Top.ToString(CultureInfo.InvariantCulture) + "," + cropDataSet.FocalPoint.Left.ToString(CultureInfo.InvariantCulture));
                cropUrl.Append("&mode=crop");
            }
            else if (crop != null && crop.Coordinates != null && preferFocalPoint == false)
            {
                cropUrl.Append("?crop=");
                cropUrl.Append(crop.Coordinates.X1.ToString(CultureInfo.InvariantCulture)).Append(",");
                cropUrl.Append(crop.Coordinates.Y1.ToString(CultureInfo.InvariantCulture)).Append(",");
                cropUrl.Append(crop.Coordinates.X2.ToString(CultureInfo.InvariantCulture)).Append(",");
                cropUrl.Append(crop.Coordinates.Y2.ToString(CultureInfo.InvariantCulture));
                cropUrl.Append("&cropmode=percentage");
            }
            else
            {
                cropUrl.Append("?anchor=center");
                cropUrl.Append("&mode=crop");
            }
            return cropUrl.ToString();
        }

        /// <summary>
        /// This tries to detect a json string, this is not a fail safe way but it is quicker than doing
        /// a try/catch when deserializing when it is not json.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static bool DetectIsJson(this string input)
        {
            input = input.Trim();
            return (input.StartsWith("{") && input.EndsWith("}"))
                   || (input.StartsWith("[") && input.EndsWith("]"));
        }
    }
}
