using OCRWebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace OCRWebApi.Controllers
{
    public class VehicleController : ApiController
    {
        IVehicleFacade _vehicleFacade;
        public VehicleController(IVehicleFacade vehicleFacade)
        {
            _vehicleFacade = vehicleFacade;
        }

        public object Post(VehicleEntity vehicle)
        {
            return _vehicleFacade.CreateVehicle(vehicle);
        }

        public object Get(string vin)
        {
           return _vehicleFacade.GetYearMakeModelByVin(vin);
        }

        public object Get(string year, string make, string model)
        {
            return _vehicleFacade.GetTaxonomyRecordsByYearMakeModel(year, make, model);
        }

    }
}
