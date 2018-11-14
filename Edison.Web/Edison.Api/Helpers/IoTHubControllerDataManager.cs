﻿using Edison.Common.Interfaces;
using Edison.Common.Messages;
using Edison.Core.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Edison.Api.Helpers
{
    public class IoTHubControllerDataManager
    {
        private readonly IMassTransitServiceBus _serviceBus;

        public IoTHubControllerDataManager(IMassTransitServiceBus serviceBus)
        {
            _serviceBus = serviceBus;
        }

        public async Task<bool> CreationDevice(DeviceCreationModel device)
        {
            var tags = new
            {
                device.DeviceType,
                device.Sensor,
                device.Geolocation,
                device.Name,
                device.Location1,
                device.Location2,
                device.Location3,
                device.Custom
            };

            await _serviceBus.BusAccess.Publish(new IoTDeviceCreateRequestedEvent()
            {
                DeviceId = device.DeviceId,
                JsonDesired = JsonConvert.SerializeObject(device.Desired),
                JsonTags = JsonConvert.SerializeObject(tags)
            });
            return true;
        }

        public async Task<bool> UpdateDevice(DeviceUpdateModel device)
        {
            var tags = new
            {
                device.DeviceType,
                device.Sensor,
                device.Geolocation,
                device.Name,
                device.Location1,
                device.Location2,
                device.Location3,
                device.Custom
            };

            await _serviceBus.BusAccess.Publish(new IoTDevicesUpdateRequestedEvent()
            {
                DeviceIds = new List<Guid>() { device.DeviceId },
                JsonDesired = JsonConvert.SerializeObject(device.Desired),
                JsonTags = JsonConvert.SerializeObject(tags)
            });
            return true;
        }

        public async Task<bool> UpdateDevicesTags(DevicesUpdateTagsModel devices)
        {
            var tags = new
            {
                devices.DeviceType,
                devices.Sensor,
                devices.Geolocation,
                devices.Name,
                devices.Location1,
                devices.Location2,
                devices.Location3,
                devices.Custom
            };

            await _serviceBus.BusAccess.Publish(new IoTDevicesUpdateRequestedEvent()
            {
                DeviceIds = devices.DeviceIds,
                JsonDesired = "",
                JsonTags = JsonConvert.SerializeObject(tags)
            });
            return true;
        }

        public async Task<bool> UpdateDevicesDesired(DevicesUpdateDesiredModel devices)
        {
            await _serviceBus.BusAccess.Publish(new IoTDevicesUpdateRequestedEvent()
            {
                DeviceIds = devices.DeviceIds,
                JsonDesired = JsonConvert.SerializeObject(devices.Desired),
                JsonTags = string.Empty,
                WaitForCompletion = true
            });
            return true;
        }

        public async Task<bool> LaunchDevicesDirectMethods(DevicesLaunchDirectMethodModel devices)
        {
            await _serviceBus.BusAccess.Publish(new IoTDevicesDirectMethodRequestedEvent()
            {
                DeviceIds = devices.DeviceIds,
                MethodName = devices.MethodName,
                MethodPayload = devices.Payload
            });
            return true;
        }

        public async Task<bool> DeleteDevice(Guid deviceId)
        {
            await _serviceBus.BusAccess.Publish(new IoTDeviceDeleteRequestedEvent()
            {
                DeviceId = deviceId
            });
            return true;
        }
    }
}
