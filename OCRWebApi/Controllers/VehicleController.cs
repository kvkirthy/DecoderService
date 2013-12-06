using OCRWebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace OCRWebApi.Controllers
{
    public class VehicleController : ApiController
    {
        IVehicleFacade _vehicleFacade;
        ILogger _log;
        public VehicleController(IVehicleFacade vehicleFacade, ILogger log)
        {
            _vehicleFacade = vehicleFacade;
            _log = log;
        }

        public object Post(VehicleEntity vehicle)
        {
            try
            {
                var stringBuilder = new StringBuilder();
                if(vehicle.photoIds != null )
                {
                    vehicle.photoIds.ToList().ForEach(p => stringBuilder.Append(p + ","));
                }

                var message = string.Format("Post a new Vehicle {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}.", vehicle.Vin, vehicle.MakeId, vehicle.Make, vehicle.ModelId, vehicle.Model, vehicle.OEMCode, (vehicle.Options == null) ? "0" : vehicle.Options.Count().ToString(), vehicle.StockNumber, vehicle.Style, vehicle.StyleId, vehicle.Trim, vehicle.Year, stringBuilder.ToString());
                _log.Log(message, System.Diagnostics.EventLogEntryType.Information);
            }
            catch
            {
                // Logging - so don't want this code to crash original worflow
            }
            

            return _vehicleFacade.CreateVehicle(vehicle);
        }

        public object Get(string vin)
        {
            _log.Log(string.Format("Get by vin {0}", vin), System.Diagnostics.EventLogEntryType.Information);
           return _vehicleFacade.GetYearMakeModelByVin(vin);
        }

        public object Get(string year, string make, string model)
        {
            _log.Log(string.Format("Get by Year - {0} /Make - {1}/Model - {2} ", year, make, model), System.Diagnostics.EventLogEntryType.Information);
            return _vehicleFacade.GetTaxonomyRecordsByYearMakeModel(year, make, model);
        }

    }
}
