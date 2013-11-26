using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCRWebApi.Models
{
    public class ReferenceDataEntity
    {
       public IEnumerable<OptionsEntity> Options { get; set; }
       public IEnumerable<ColorReferenceEntity> Colors { get; set; }
    }
}