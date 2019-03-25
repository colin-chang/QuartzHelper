using System;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Xunit;
using Xunit.Abstractions;

namespace ColinChang.QuartzHelper.Test
{
    public class QuartzTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Action _job;
        private readonly string _cron;

        public QuartzTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _job = () => _testOutputHelper.WriteLine(DateTime.Now.ToString());
            _cron = "0/2 * * * * ? *";
        }

        [Fact]
        public async Task ScheduleSimpleJobAsyncTest()
        {
            await QuartzHelper.ScheduleSimpleJobAsync("ScheduleSimpleJob", _job, _cron);

            await Task.Delay(10000);
        }


        [Fact]
        public async Task ScheduleCustomJobAsyncTest()
        {
            var job = QuartzHelper.CreateSimpleJob("ScheduleCustomJob", "ScheduleCustomJobGroup", _job);
            var trigger = QuartzHelper.CreateTrigger("ScheduleCustomTrigger", "ScheduleCustomTriggerGroup", _cron);

            await QuartzHelper.GetScheduler().ScheduleJob(job, trigger);
            await Task.Delay(10000);
        }
        
        [Fact]
        public async Task ScheduleOnceJobAsyncTest()
        {
            var job = QuartzHelper.CreateSimpleJob("ScheduleOnceJob", "ScheduleOnceJobGroup", _job);
            var trigger = QuartzHelper.CreateTrigger(new TriggerKey("ScheduleOnceTrigger"),DateTime.Now.AddSeconds(2));

            await QuartzHelper.GetScheduler().ScheduleJob(job, trigger);
            await Task.Delay(3000);
        }
    }
}