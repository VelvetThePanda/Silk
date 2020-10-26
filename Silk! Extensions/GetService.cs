﻿using System;

namespace Silk__Extensions
{
    public static class GetService
    {
        public static T Get<T>(this IServiceProvider provider) => (T)provider.GetService(typeof(T));
    }
}