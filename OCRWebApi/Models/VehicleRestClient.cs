using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCRWebApi.Models
{
    public class VehicleRestClient : IVehicleFacade
    {
        public VehicleRestClient()
        {

        }

        /// <summary>
        /// Obtain vehicle record by VIN
        /// </summary>
        /// <param name="vin">The VIN</param>
        /// <returns>Vehicle object associated with the VIN</returns>
        public VehicleEntity GetYearMakeModelByVin(string vin)
        {
            //TODO: pull URL from configuration file.
            var requestUri = "https://api.dev-2.cobalt.com/inventory/rest/v1.0/vehicles/detail?inventoryOwner=gmps-kindred&locale=en_us";
            string jsonMessage = "{\"vehicles\":[{\"vehicle\":{\"vin\":\""+ vin +"\"}}]}";
            return GetVehicleEntityByParsingJSON(RestClient.PostData(requestUri, jsonMessage));
        }
        
        /// <summary>
        /// Get Vehicle Taxonomy possibilities by Year, Make and Model
        /// </summary>
        /// <param name="year">The Year</param>
        /// <param name="make">The Make </param>
        /// <param name="model">The Model</param>
        /// <returns>Taxonomy list</returns>
        public IEnumerable<VehicleEntity> GetTaxonomyRecordsByYearMakeModel(string year, string make, string model)
        {
            //TODO: pull URL from configuration file.
            var requestUri = string.Format("https://api.dev-2.cobalt.com/inventory/rest/v1.0/taxonomy/search?inventoryLocale=en_us&inventoryOwner=gmps-kindred&make={1}&model={2}&year={0}",year,make,model);
            return GetTaxonomyListByParsingJson(RestClient.GetData(requestUri));
        }

        private IEnumerable<VehicleEntity> GetTaxonomyListByParsingJson(JObject json)
        {
            var taxonomyResults = new List<VehicleEntity>();
            var responseObject = json.ToObject<dynamic>();

            if (responseObject != null)
            {
                if (responseObject.searchResult != null)
                {
                    if(responseObject.searchResult.taxonomy != null 
                        && responseObject.searchResult.taxonomy.GetType() == typeof(JArray))
                    {
                        foreach (var taxonomyObject in responseObject.searchResult.taxonomy)
                        {
                            if(taxonomyObject.taxonomyRecord != null)
                            {
                                var vehicleEntity = new VehicleEntity();
                                TryGetMake(taxonomyObject.taxonomyRecord, vehicleEntity);
                                TryGetModel(taxonomyObject.taxonomyRecord, vehicleEntity);
                                TryGetYear(taxonomyObject.taxonomyRecord, vehicleEntity);
                                TryGetOemModelCode(taxonomyObject.taxonomyRecord, vehicleEntity);
                                TryGetTrimAndStyle(taxonomyObject.taxonomyRecord, vehicleEntity);
                                taxonomyResults.Add(vehicleEntity);
                            }
                        }
                    }
                }
            }

            return taxonomyResults;

        }

        /// <summary>
        /// Mapper for transforming incoming object to the one service returns
        /// </summary>
        /// <param name="responseJson">JSON to be parsed</param>
        /// <returns>Resultant object structure of vehicle</returns>
        private VehicleEntity GetVehicleEntityByParsingJSON(JObject responseJson)
        {
            var response = responseJson.ToObject<dynamic>();
            var returnEntity = new VehicleEntity();

            if(response !=null)
            {
                if(response.vehicles != null && response.vehicles.GetType() == typeof(JArray)
                    && response.vehicles[0].vehicle != null)
                {                    
                    TryGetMake(response.vehicles[0].vehicle, returnEntity);
                    TryGetModel(response.vehicles[0].vehicle, returnEntity);
                    TryGetYear(response.vehicles[0].vehicle, returnEntity);
                }
            }

            return returnEntity;
            
        }

        #region Parse individual Vehicle Objects from JSON
            private void TryGetMake(dynamic jsonObject, VehicleEntity vehicle)
            {
                try
                {
                    if (vehicle == null) vehicle = new VehicleEntity();

                    if (jsonObject.make != null
                            && jsonObject.make.label != null
                            && jsonObject.make.id != null
                            && jsonObject.make.label.Value != null
                            && jsonObject.make.id.Value != null)
                    {

                        vehicle.Make = jsonObject.make.label.Value.ToString();
                        vehicle.MakeId = jsonObject.make.id.Value.ToString();
                    }
                }
                catch
                {
                    //TODO: take care of logging.
                }

            }
            private void TryGetModel(dynamic jsonObject, VehicleEntity vehicle)
            {
                try
                {
                    if (vehicle == null) vehicle = new VehicleEntity();

                    if (jsonObject.model != null
                            && jsonObject.model.label != null
                            && jsonObject.model.id != null
                            && jsonObject.model.label.Value != null
                            && jsonObject.model.id.Value != null)
                    {

                        vehicle.Model = jsonObject.model.label.Value.ToString();
                        vehicle.ModelId = jsonObject.model.id.Value.ToString();
                    }
                }
                catch
                {
                    //TODO: Logging
                }

            }
            private void TryGetYear(dynamic jsonObject, VehicleEntity vehicle)
            {
                try
                {
                    if (vehicle == null) vehicle = new VehicleEntity();

                    if (jsonObject.year != null
                                && jsonObject.year.Value != null)
                    {
                        int year;
                        Int32.TryParse(jsonObject.year.Value.ToString(), out year);
                        vehicle.Year = new DateTime(year, 1, 1);

                    }
                }
                catch
                {
                    //TODO: Logging
                }
            }
            private void TryGetOemModelCode(dynamic jsonObject, VehicleEntity vehicle)
            {
                try
                {
                    if (vehicle == null) vehicle = new VehicleEntity();

                    if (jsonObject.oemModelCode != null
                                && jsonObject.oemModelCode.Value != null)
                    {
                        vehicle.OEMCode = jsonObject.oemModelCode.Value;
                    }
                }
                catch
                {
                    //TODO: Logging
                }

            }
            private void TryGetTrimAndStyle(dynamic jsonObject, VehicleEntity vehicle)
            {
                try
                {
                    if (vehicle == null) vehicle = new VehicleEntity();

                    if (jsonObject.style != null
                            && jsonObject.style.label != null
                            && jsonObject.style.id != null
                            && jsonObject.style.trim != null
                            && jsonObject.style.label.Value != null
                            && jsonObject.style.id.Value != null
                            && jsonObject.style.trim.Value!= null)
                    {

                        vehicle.Style = jsonObject.style.label.Value.ToString();
                        vehicle.StyleId = jsonObject.style.id.Value.ToString();
                        vehicle.Trim = jsonObject.style.trim.Value.ToString();
                    }
                }
                catch
                {
                    //TODO: take care of logging.
                }

            }

        #endregion Parse individual Vehicle Objects from JSON
    }
}