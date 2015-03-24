using System.ComponentModel.DataAnnotations;
using NLog;
using NLog.Layouts;
using NLog.Targets;
using Newtonsoft.Json;

namespace Gelf4NLog.Target
{
    [Target("GrayLog")]
    public class NLogTarget : TargetWithLayout
    {
        [Required]
        public Layout HostIp { get; set; }

        [Required]
        public Layout HostPort { get; set; }

        public string Facility { get; set; }

        public IConverter Converter { get; private set; }
        public ITransport Transport { get; private set; }

        private int _port;
        private string _hostIp;

        public NLogTarget()
        {
            Transport = new UdpTransport(new UdpTransportClient());
            Converter = new GelfConverter();
        }

        public NLogTarget(ITransport transport, IConverter converter)
        {
            Transport = transport;
            Converter = converter;
        }

        public void WriteLogEventInfo(LogEventInfo logEvent)
        {
            Write(logEvent);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var jsonObject = Converter.GetGelfJson(logEvent, Facility);
            if (jsonObject == null) return;

            if (string.IsNullOrWhiteSpace(_hostIp))
            {
                _hostIp = HostIp.Render(logEvent);
                _port = int.Parse(HostPort.Render(logEvent));
            }

            Transport.Send(_hostIp, _port, jsonObject.ToString(Formatting.None, null));
        }
    }
}
