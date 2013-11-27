using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCRWebApi.Models
{
    //TODO: All if's while parsing need to have else with error logged when parsing fails.
    public class VehicleRestClient : IVehicleFacade
    {
        public VehicleRestClient()
        {

        }

        public VehicleEntity CreateVehicle(VehicleEntity newVehicle)
        {
            var resultVehicleEntity = new VehicleEntity(); 

            #region Detailer Call
            //TODO: move uri to configuration file
            var requestUri = "https://api.dev-2.cobalt.com/inventory/rest/v1.0/vehicles/detail?inventoryOwner=gmps-kindred&locale=en_us";
            dynamic detailerResponse = RestClient.PostData(requestUri, GetDetailerRequestPayload(newVehicle));
            #endregion Detailer call

            #region Create Vehicle Call
                        if (detailerResponse.vehicles != null && detailerResponse.vehicles.GetType() == typeof(JArray)
                            && detailerResponse.vehicles[0] != null && detailerResponse.vehicles[0].vehicle != null)
                        {
                            JObject vehicleEntity = detailerResponse.vehicles[0].vehicle;
                            vehicleEntity.Add("stockNumber", newVehicle.StockNumber ?? string.Empty);
                            var createVehicleRequestPayload = string.Format("{{\"criteria\":{{\"vehicleContexts\":[{{\"vehicleContext\":{{\"vehicle\":{0},\"modifiedFields\":[\"assets\",\"bodyStyle\",\"bodyType\",\"certified\",\"colors.exterior.base\",\"colors.exterior.code\",\"colors.exterior.name\",\"colors.interior.code\",\"colors.interior.name\",\"createdDate\",\"descriptions\",\"doors\",\"drivetrain\",\"engine.aspiration\",\"engine.cylinders\",\"engine.description\",\"engine.displacement\",\"engine.fuelType\",\"engine.power\",\"id\",\"inventoryOwner\",\"lastModifiedDate\",\"lotDate\",\"make.Id\",\"make.label\",\"model.Id\",\"model.label\",\"odometer\",\"oemModelCode\",\"options.dealerOptions\",\"options.factoryOptions\",\"preOwned\",\"prices.discountPrice\",\"prices.internetPrice\",\"prices.invoicePrice\",\"prices.msrp\",\"prices.retailPrice\",\"prices.vendedPrice\",\"stockNumber\",\"style.Id\",\"style.trim\",\"transmission.speeds\",\"transmission.text\",\"transmission.type\",\"unmodifiable\",\"vin\",\"warranties\",\"year\"]}}}}],\"inventoryOwner\":\"gmps-kindred\"}}}}", 
                                vehicleEntity);
                            //TODO: move uri to configuration file
                            dynamic result = RestClient.PostData("https://api.dev-2.cobalt.com/inventory/rest/v1.0/vehicles?inventoryOwner=gmps-kindred", createVehicleRequestPayload);

                            if (result != null && result.result != null)
                            {
                                result = result.result;
                                if (result != null
                                    && result.status != null
                                    && result.status.GetType() == typeof(JArray)
                                    && result.status[0].vehicle != null)
                                {
                                    TryGetMake(result.status[0].vehicle, resultVehicleEntity);
                                    TryGetModel(result.status[0].vehicle, resultVehicleEntity);
                                    TryGetOemModelCode(result.status[0].vehicle, resultVehicleEntity);
                                    TryGetTrimAndStyle(result.status[0].vehicle, resultVehicleEntity);
                                    TryGetYear(result.status[0].vehicle, resultVehicleEntity);

                                    #region Get Color from response
                                    var refStyles = result.status[0].vehicle;
                                    if (refStyles.colors != null && refStyles.colors.GetType() == typeof(JArray))
                                    {
                                        foreach (var iColor in refStyles.colors)
                                        {
                                            var color = iColor.color;
                                            var colorRefObject = new ColorReferenceEntity();

                                            if (color.category != null && color.category == "Exterior")
                                            {
                                                resultVehicleEntity.ExternalColor = new Color
                                                {
                                                    Code = color.code ?? string.Empty,
                                                    //Base = color.exterior.base ?? string.Empty,
                                                    Name = color.name ?? string.Empty,
                                                    RgbHexCode = color.RGBHexCode ?? string.Empty
                                                };
                                            }

                                            if (color.category != null && color.category == "Interior")
                                            {
                                                resultVehicleEntity.InternalColor = new Color
                                                {
                                                    Code = color.code ?? string.Empty,
                                                    //Base = color.exterior.base ?? string.Empty,
                                                    Name = color.name ?? string.Empty,
                                                    RgbHexCode = color.RGBHexCode ?? string.Empty
                                                };                                          
                                            }                                            
                                        }
                                    }
                                   
                                    #endregion Get Color from response
                                }
                            }
                        }
            
            #endregion Create Vehicle Call
            
            return resultVehicleEntity;

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

        public ReferenceDataEntity GetOptionsByStyleId(string styleId)
        {
            //TODO: pull URL from configuration file.
            var requestUri = string.Format("https://api.dev-2.cobalt.com/inventory/rest/v1.0/reference/search?inventoryLocale=en_us&inventoryOwner=gmps-kindred&loadColors=true&styleId={0}", styleId);
            var jsonResult = RestClient.GetData(requestUri);
            return new ReferenceDataEntity{
                Colors = GetColorReferenceEntities(jsonResult),
                Options = GetOptionsListByParsingJson(jsonResult)
            };            
        }

        private string GetDetailerRequestPayload(VehicleEntity newVehicle)
        {
            string colorPayload = "\"colors\":[{0}]", optionsPayload = null;

            var basicVehiclePayload = "\"vin\":\"" + (newVehicle.Vin ?? string.Empty) + "\",\"year\":" + (newVehicle.Year.ToString() ?? string.Empty) + ",\"make\":{\"id\":" + (newVehicle.MakeId ?? string.Empty) + ",\"label\":\"" + (newVehicle.Make ?? string.Empty) +
                "\"},\"model\":{\"id\":" + (newVehicle.ModelId ?? string.Empty) + ",\"label\":\"" + (newVehicle.Model ?? string.Empty) + "\"},\"style\":{\"id\":" + (newVehicle.StyleId ?? string.Empty) + ",\"label\":\"" + (newVehicle.Style ?? string.Empty) + "\",\"trim\":\"" + (newVehicle.Trim ?? string.Empty) + "\"},\"oemModelCode\":\"" + (newVehicle.OEMCode ?? string.Empty) + "\"";//}]}";

            if (newVehicle.ExternalColor != null && newVehicle.InternalColor != null)
            {
                //TODO: using name for base color too. might need to fix it.
                var twoColorsOfTheVehicle = string.Format("{{\"color\":{{\"category\":\"Exterior\",\"name\":\"{0}\",\"base\":\"{0}\",\"code\":\"{1}\"}} }},{{\"color\":{{\"code\":\"{2}\",\"name\":\"{2}\",\"category\":\"Interior\"}} }}", newVehicle.ExternalColor.Name ?? string.Empty, newVehicle.ExternalColor.Code ?? string.Empty, newVehicle.InternalColor.Code ?? string.Empty, newVehicle.InternalColor.Name ?? string.Empty);
                colorPayload = string.Format(colorPayload, twoColorsOfTheVehicle);
            }
            else
            {
                colorPayload = string.Format(colorPayload, string.Empty);
            }

            string factoryOptionsArray = string.Empty;
            if (newVehicle.Options != null)
            {
                factoryOptionsArray = "\"factoryOptions\":[";
                foreach (var option in newVehicle.Options)
                {
                    var factoryOptionEntity = string.Format("{\"id\":4,\"optionCode\":\"{0}\",\"description\":\"{1}\"}", option.OptionCode, option.Description);
                    factoryOptionsArray = string.Format("{0},{1}", factoryOptionsArray, factoryOptionEntity);
                }
                factoryOptionsArray = string.Format("{0}]", factoryOptionsArray);
            }
            optionsPayload = string.Format("\"options\":{{{0}}}", factoryOptionsArray);

            return string.Format("{{\"vehicles\":[{{\"vehicle\":{{{0},{1},{2}}} }}]}}", basicVehiclePayload, colorPayload, optionsPayload);
        }

        //TODO: Still need to cleanup Colors. They could be repeated now.
        private IEnumerable<ColorReferenceEntity> GetColorReferenceEntities(JObject json)
        {
            var colorResults = new List<ColorReferenceEntity>();
            var responseObject = json.ToObject<dynamic>();

            if (responseObject != null)
            {
                if (responseObject.searchResult != null)
                {
                    if (responseObject.searchResult.referenceStyles != null
                        && responseObject.searchResult.referenceStyles != null
                        && responseObject.searchResult.referenceStyles.GetType() == typeof(JArray))
                    {
                        foreach (var refStyles in responseObject.searchResult.referenceStyles)
                        {
                            TryGetColors(refStyles, colorResults);
                        }
                    }
                }
            }

            return colorResults;
        }

        private IEnumerable<OptionsEntity> GetOptionsListByParsingJson(JObject json)
        {
            var optionsResults = new List<OptionsEntity>();
            var responseObject = json.ToObject<dynamic>();

            if (responseObject != null)
            {
                if (responseObject.searchResult != null)
                {
                    if (responseObject.searchResult.referenceStyles != null
                        && responseObject.searchResult.referenceStyles != null
                        && responseObject.searchResult.referenceStyles.GetType() == typeof(JArray))
                    {
                        foreach (var option in responseObject.searchResult.referenceStyles)
                        {
                            if (option.options != null
                                && option.options.factoryOptions != null
                                && option.options.factoryOptions.GetType() == typeof(JArray))
                            {
                                foreach (var factoryOption in option.options.factoryOptions)
                                {
                                    if (factoryOption.optionCode != null
                                        && factoryOption.optionCode.Value != null
                                        && factoryOption.description != null
                                        && factoryOption.description.Value != null)
                                    {
                                        optionsResults.Add(new OptionsEntity
                                        {
                                            OptionCode = factoryOption.optionCode.Value,
                                            Description = factoryOption.description.Value
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return optionsResults;
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
                        vehicle.Year = year;
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

            private void TryGetColors(dynamic refStyles, List<ColorReferenceEntity> colorResults)
            {
                if (refStyles.colors != null && refStyles.colors.GetType() == typeof(JArray))
                {
                    foreach (var color in refStyles.colors)
                    {                        
                        var colorRefObject = new ColorReferenceEntity();                        

                        if (color.exterior != null )
                        {
                            var colorObject = new Color
                            {
                                Code = color.exterior.code ?? string.Empty,
                                //Base = color.exterior.base ?? string.Empty,
                                Name = color.exterior.name ?? string.Empty,
                                RgbHexCode = color.exterior.RGBHexCode ?? string.Empty
                            };
                            colorRefObject.ExternalColor = colorObject;

                        }
                        if (color.interior != null )
                        {
                            var colorObject = new Color
                            {
                                Code = color.interior.code ?? string.Empty,
                                //Base = color.exterior.base ?? string.Empty,
                                Name = color.interior.name ?? string.Empty,
                                RgbHexCode = color.interior.RGBHexCode ?? string.Empty
                            };
                            colorRefObject.InternalColor = new List<Color>(){
                                            colorObject
                                        };
                        }

                        colorResults.Add(colorRefObject);
                    }

                }

            }

        #endregion Parse individual Vehicle Objects from JSON
            
    }
}