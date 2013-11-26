using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCRWebApi.Models
{
    public class ColorReferenceEntity
    {
        public Color ExternalColor { get; set; }
        public IEnumerable<Color> InternalColor { get; set; }
    }
}