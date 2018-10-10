﻿using Edison.Core.Common.Models;
using System;

namespace Edison.Common.Messages.Interfaces
{
    public interface IDeviceCreateOrUpdateRequested : IMessage
    {
        Guid DeviceId { get; set; }
        string ChangeType { get; set; }
        DateTime Date { get; set; }
        string Data { get; set; }
    }
}
