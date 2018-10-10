﻿using Edison.Common.Messages.Interfaces;
using Edison.Core.Common.Models;
using System;

namespace Edison.Common.Messages
{
    public class DeviceDeleteRequestedEvent : IDeviceDeleteRequested
    {
        public Guid DeviceId { get; set; }
    }
}
