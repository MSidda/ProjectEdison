﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MassTransit;
using Edison.Core.Interfaces;
using Edison.Core.Common.Models;
using Edison.Common.Interfaces;
using Edison.Common.Messages.Interfaces;
using Edison.Common.Messages;
using Newtonsoft.Json;

namespace Edison.ResponseService.Consumers
{
    /// <summary>
    /// Masstransit consumer that handles the light sensor action from a response
    /// </summary>
    public class ResponseActionLightSensorEventConsumer : ResponseActionBaseConsumer, IConsumer<IActionLightSensorEvent>
    {
        private readonly IDeviceRestService _deviceRestService;
        private readonly IIoTHubControllerRestService _iotHubControllerRestService;
        private readonly IMassTransitServiceBus _serviceBus;
        private List<string> _colors = new List<string>()
        {
            "off", "red", "green", "blue", "cyan", "yellow", "purple", "white"
        };
        private List<string> _states = new List<string>()
        {
            "off", "on", "flashing"
        };

        public ResponseActionLightSensorEventConsumer(IDeviceRestService deviceRestService,
            IResponseRestService responseRestService,
            IIoTHubControllerRestService iotHubControllerRestService,
            IMassTransitServiceBus serviceBus,
            ILogger<ResponseActionLightSensorEventConsumer> logger) : base(responseRestService, logger)
        {
            _deviceRestService = deviceRestService;
            _iotHubControllerRestService = iotHubControllerRestService;
            _serviceBus = serviceBus;
        }

        public async Task Consume(ConsumeContext<IActionLightSensorEvent> context)
        {
            DateTime actionStartDate = DateTime.UtcNow;
            try
            {
                if (context.Message != null && context.Message as IActionLightSensorEvent != null)
                {
                    IActionLightSensorEvent action = context.Message;
                    _logger.LogDebug($"ResponseActionLightSensorEventConsumer: ActionId: '{action.ActionId}'.");

                    //Return skipped, there is no location set for the response
                    if(action.GeolocationPoint == null)
                    {
                        await GenerateActionCallback(context, ActionStatus.Skipped, actionStartDate, "The response has no location. This action can not be executed.");
                        _logger.LogDebug("ResponseActionLightSensorEventConsumer: The response has no location. This action can not be executed.");
                        return;
                    }

                    //Get devices in radius
                    IEnumerable<Guid> devicesInRadius = await _deviceRestService.GetDevicesInRadius(new DeviceGeolocationModel()
                    {
                        DeviceType = "Lightbulb",
                        Radius = action.PrimaryRadius,
                        ResponseGeolocationPointLocation = action.GeolocationPoint
                    });

                    if(action.RadiusType == "secondary")
                    {
                        IEnumerable<Guid> devicesInSecondaryRadius = await _deviceRestService.GetDevicesInRadius(new DeviceGeolocationModel()
                        {
                            DeviceType = "Lightbulb",
                            Radius = action.SecondaryRadius,
                            ResponseGeolocationPointLocation = action.GeolocationPoint
                        });
                        devicesInRadius = devicesInSecondaryRadius.Except(devicesInRadius);
                    }

                    if(devicesInRadius.ToList().Count == 0)
                    {
                        await GenerateActionCallback(context, ActionStatus.Success, actionStartDate);
                        _logger.LogDebug($"ResponseActionLightSensorEventConsumer: No light in radius.");
                        return;
                    }

                    //State
                    string state = string.Empty;
                    action.State = action.State.ToLower();
                    if (!_states.Contains(action.State))
                        state = "off";
                    else
                        state = action.State;

                    //Color
                    string color = string.Empty;
                    action.Color = action.Color.ToLower();
                    if (!_colors.Contains(action.Color))
                        color = "off";
                    else
                        color = action.Color;

                    //Flash Frequency
                    int frequency = 0;
                    if(action.State == "flashing")
                    {
                        frequency = action.FlashFrequency;
                    }

                    //Run the job
                    var resultMessage = await context.Request<IIoTDevicesUpdateRequested, IIoTDevicesUpdated>(_serviceBus.BusAccess, new IoTDevicesUpdateRequestedEvent()
                    {
                        DeviceIds = devicesInRadius.ToList(),
                        JsonDesired = JsonConvert.SerializeObject(new Dictionary<string, object>()
                        {
                            { "State", state },
                            { "Color", color },
                            { "FlashFrequency", frequency }
                        }),
                        JsonTags = string.Empty,
                        WaitForCompletion = true
                    });
                    bool result = resultMessage != null & resultMessage.Message != null;

                    /*bool result = await _iotHubControllerRestService.UpdateDevicesDesired(new DevicesUpdateDesiredModel()
                    {
                        DeviceIds = devicesInRadius.ToList(),
                        Desired = new Dictionary<string, object>()
                        {
                            { "State", state },
                            { "Color", color },
                            { "FlashFrequency", frequency }
                        }
                    });*/

                    //Success
                    if (result)
                    {
                        await GenerateActionCallback(context, ActionStatus.Success, actionStartDate);
                        _logger.LogDebug($"ResponseActionLightSensorEventConsumer: Desired properties applied properly.");
                        return;
                    }

                    //Error
                    await GenerateActionCallback(context, ActionStatus.Error, actionStartDate, $"Action '{action.ActionId}': Desired properties not applied properly.");
                    _logger.LogError("ResponseActionLightSensorEventConsumer: Desired properties not applied properly.");
                }
            }
            catch (Exception e)
            {
                await GenerateActionCallback(context, ActionStatus.Error, actionStartDate, $"Action '{context?.Message?.ActionId}': {e.Message}.");
                _logger.LogError($"ResponseActionLightSensorEventConsumer: {e.Message}");
                throw e;
            }
        }
    }
}
