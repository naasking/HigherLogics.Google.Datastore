﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Google.Cloud.Datastore.V1.Mapper
{
    [StructLayout(LayoutKind.Explicit)]
    struct Union
    {
        //FIXME: is this endian sensitive?
        [FieldOffset(0)] public long[] L;
        [FieldOffset(0)] public int[] I;
    }
}
