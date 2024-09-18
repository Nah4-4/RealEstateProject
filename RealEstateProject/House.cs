using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateProject
{       
    public class House
    {
       public int PropertyId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public int SizeInSqft { get; set; }
        public string Address { get; set; }
        public string ImagePath { get; set; }
    }
}
