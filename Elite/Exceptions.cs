// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;

namespace Elite
{
    class CovenantDisconnectedException : Exception
    {
        public CovenantDisconnectedException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}
