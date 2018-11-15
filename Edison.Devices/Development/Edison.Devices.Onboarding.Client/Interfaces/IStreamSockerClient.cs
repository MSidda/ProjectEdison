﻿using System.Threading.Tasks;
using Edison.Devices.Onboarding.Common.Models;

namespace Edison.Devices.Onboarding.Client.Interfaces
{
    public interface IStreamSockerClient
    {
        Task<U> SendCommand<T, U>(CommandsEnum requestCommandType, T parameters, string passkey) where T : RequestCommand where U : ResultCommand;
        Task<T> SendCommand<T>(CommandsEnum requestCommandType, string passkey) where T : ResultCommand;
    }
}
