using System;

namespace ColinChang.QuartzHelper.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            var key = "test";
            var jt= key.ToJobTrigger();

            var trigger = QuartzHelper.CreateTrigger(jt.Trigger, "0/2 * * * * ?",DateTime.UtcNow,DateTime.UtcNow.AddSeconds(6));
            // var trigger = QuartzHelper.CreateTrigger(jt.Trigger, "0/2 * * * * ?",DateTime.Now);
            var job = QuartzHelper.CreateSimpleJob(jt.Job, () => Console.WriteLine(DateTime.Now));
            QuartzHelper.GetScheduler().ScheduleJob(job,trigger);
             
            Console.ReadKey();
        }
    }
}