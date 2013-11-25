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

        public VehicleEntity GetVehicleByVin(string vin)
        {
            //var requestUri = "https://api.dev-2.cobalt.com/inventory/rest/v1.0/taxonomy/search?inventoryLocale=en_us&inventoryOwner=gmps-kindred&make=Ford&model=Fiesta&year=2012";
            //TODO: pull URL from configuration file.
            //sample vin - 3FADP4BJ8CM184301
            var requestUri = "https://api.dev-2.cobalt.com/inventory/rest/v1.0/vehicles/detail?inventoryOwner=gmps-kindred&locale=en_us";
            string jsonMessage = "{\"vehicles\":[{\"vehicle\":{\"vin\":\""+ vin +"\"}}]}";
            return GetVehicleEntityByParsingJSON(RestClient.PostData(requestUri, jsonMessage));
        }

        private VehicleEntity GetVehicleEntityByParsingJSON(JObject responseJson)
        {
            var response = responseJson.ToObject<dynamic>();
            var returnEntity = new VehicleEntity();

            if(response !=null)
            {
                if(response.vehicles != null && response.vehicles.GetType() == typeof(JArray)
                    && response.vehicles[0].vehicle != null)
                {
                    #region Make Transform
                    if (response.vehicles[0].vehicle.make != null 
                        && response.vehicles[0].vehicle.make.label != null
                        && response.vehicles[0].vehicle.make.id != null 
                        && response.vehicles[0].vehicle.make.label.Value != null
                        && response.vehicles[0].vehicle.make.id.Value != null 
                        )
                    {
                        returnEntity.Make = response.vehicles[0].vehicle.make.label.Value.ToString();
                        returnEntity.MakeId = response.vehicles[0].vehicle.make.id.Value.ToString();
                    }
                    #endregion Make Transform

                    #region Model Transform
                    if (response.vehicles[0].vehicle.model != null
                        && response.vehicles[0].vehicle.model.label != null
                        && response.vehicles[0].vehicle.model.id != null
                        && response.vehicles[0].vehicle.model.label.Value != null
                        && response.vehicles[0].vehicle.model.id.Value != null
                        )
                    {
                        returnEntity.Model = response.vehicles[0].vehicle.model.label.Value.ToString();
                        returnEntity.ModelId = response.vehicles[0].vehicle.model.id.Value.ToString();
                    }
                    #endregion Model transform

                    #region Year Transform
                    if (response.vehicles[0].vehicle.year != null
                        && response.vehicles[0].vehicle.year.Value != null)
                    {
                        int year;
                        Int32.TryParse(response.vehicles[0].vehicle.year.Value.ToString(), out year);
                        returnEntity.Year = new DateTime(year,1,1);

                    }
                    #endregion Year Transform

                }
            }

            return returnEntity;
            
        }
    }
}