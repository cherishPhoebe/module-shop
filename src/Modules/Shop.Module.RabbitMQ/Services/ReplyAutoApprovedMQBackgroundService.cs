﻿using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Module.MQ.Abstractions.Data;
using Shop.Module.MQ.Abstractions.Services;
using Shop.Module.Reviews.Abstractions.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shop.Module.RabbitMQ.Services
{
    public class ReplyAutoApprovedMQBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IMQService _mqService;
        private readonly IServiceProvider _serviceProvider;
        //private readonly IMediator _mediator; //切记不可注入使用

        public ReplyAutoApprovedMQBackgroundService(
            ILogger<ReplyAutoApprovedMQBackgroundService> logger,
            IMQService mqService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _mqService = mqService;
            _serviceProvider = serviceProvider;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var success = _mqService.DirectReceive<ReplyAutoApprovedEvent>(QueueKeys.ReplyAutoApproved, async c =>
            {
                try
                {
                    if (stoppingToken.IsCancellationRequested)
                        throw new Exception("canceled");
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        await mediator.Publish(c);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("消息处理失败", ex, c);
                }
            }, out string message);
            if (!success)
            {
                _logger.LogError("消息接收异常", message);
            }
            await Task.CompletedTask;

            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            //}
            //_logger.LogInformation(nameof(ReplyAutoApprovedMQBackgroundService) + " stopping...");
        }
    }
}
