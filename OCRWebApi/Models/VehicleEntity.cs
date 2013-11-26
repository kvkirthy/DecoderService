using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCRWebApi.Models
{
    public class VehicleEntity
    {        
        public string Vin { get; set; }
        public string StockNumber { get; set; }
        public DateTime Year { get; set; }
        public string MakeId { get; set; }
        public string Make { get; set; }
        public string ModelId { get; set; }
        public string Model { get; set; }
        public string Trim { get; set; }
        public string StyleId { get; set; }
        public string Style { get; set; }
        public string OEMCode { get; set; }
        public IEnumerable<OptionsEntity> Options{ get; set; }
        public Color ExternalColor { get; set; }
        public Color InternalColor { get; set; }

    }
}