using System;
using System.Collections.Generic;
using System.Text;

namespace Qx.Internals
{
    public static partial class Prelude
    {
        public class Unit
        {
			public static readonly Unit Default = new Unit();
            private Unit() { }
        }
    }
}
