using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    internal static class StringExtensions
    {
        public static Image ConvertBase64ToImage(this string base64) => (Bitmap)new ImageConverter().ConvertFrom(Convert.FromBase64String(base64));
    }
}
