using OCRWebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace OCRWebApi.Controllers
{
    public class OptionsController : ApiController
    {
     
        IVehicleFacade _vehicleFacade;
        public OptionsController(IVehicleFacade vehicleFacade)
        {
            _vehicleFacade = vehicleFacade;
        }

        public object Get(string styleCode)
        {
            return _vehicleFacade.GetOptionsByStyleId(styleCode);
        }
        
    }
}
