﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MassTransit;
using Edison.Core.Interfaces;
using Edison.Core.Common.Models;
using Edison.Common.Messages.Interfaces;
using Edison.Common.Messages;

namespace Edison.ResponseService.Consumers
{
    /// <summary>
    /// Masstransit consumer that handles a generic action from a response
    /// </summary>
    public class ResponseActionEventConsumer : ResponseActionBaseConsumer, IConsumer<IActionEvent>
    {
        public ResponseActionEventConsumer(IResponseRestService responseRestService,
            ILogger<ResponseActionEventConsumer> logger) : base (responseRestService, logger)
        {
        }

        public async Task Consume(ConsumeContext<IActionEvent> context)
        {
            try
            {
                _logger.LogDebug($"ResponseActionEventConsumer: Retrieved message from response '{context.Message.Action.ActionId}'.");
                ResponseActionModel action = context.Message.Action;
                if (!string.IsNullOrEmpty(action.ActionType))
                {
                    _logger.LogDebug($"ResponseActionEventConsumer: ActionType retrieved: '{action.ActionType}'.");
                    //Switch statement directing different action types
                    switch(action.ActionType)
                    {
                        case "notification":
                            _logger.LogDebug($"ResponseActionEventConsumer: Publish ActionNotificationEvent.");
                            await context.Publish(new ActionNotificationEvent(action, context.Message.IsCloseAction) { ResponseId = context.Message.ResponseId });
                            break;
                        case "lightsensor":
                            _logger.LogDebug($"ResponseActionEventConsumer: Publish ActionLightSensorEvent.");
                            await context.Publish(new ActionLightSensorEvent(action, context.Message.IsCloseAction) {
                                 ResponseId = context.Message.ResponseId,
                                 GeolocationPoint = context.Message.Geolocation,
                                 PrimaryRadius = context.Message.PrimaryRadius,
                                 SecondaryRadius = context.Message.SecondaryRadius
                            });
                            break;
                        default:
                            await GenerateActionCallback(context, ActionStatus.Skipped, DateTime.UtcNow, $"Action '{action.ActionId}': The action type '{action.ActionType}' cannot be handled.");
                            break;
                    }
                    return;
                }
                _logger.LogError("ResponseActionEventConsumer: Invalid Null or Empty Action Type");
                throw new Exception("Invalid Null or Empty Action Type");

            }
            catch (Exception e)
            {
                _logger.LogError($"ResponseActionEventConsumer: {e.Message}");
                throw e;
            }
        }
    }
}
