using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCRWebApi.Models
{
    public interface IVehicleFacade
    {
        VehicleEntity GetVehicleByVin(string vin);
    }
}
