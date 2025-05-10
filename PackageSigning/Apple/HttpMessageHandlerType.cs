//
// HttpMessageHandler.cs
//
// Author: Vincent Dondain <vincent.dondain@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (www.xamarin.com)
//

using System;

namespace Xamarin.MacDev;

[Flags]
public enum HttpMessageHandlerType
{
    HttpClientHandler,
    CFNetworkHandler,
    NSUrlSessionHandler
}