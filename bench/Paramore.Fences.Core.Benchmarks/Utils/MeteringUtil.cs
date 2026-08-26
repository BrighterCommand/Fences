using System.Diagnostics.Metrics;

namespace Paramore.Fences.Core.Benchmarks.Utils;

internal class MeteringUtil
{
    public static MeterListener ListenFencesMetrics()
    {
        var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name is "Paramore.Fences")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
        meterListener.Start();

        static void OnMeasurementRecorded<T>(
            Instrument instrument,
            T measurement,
            ReadOnlySpan<KeyValuePair<string, object?>> tags,
            object? state)
        {
            // do nothing
        }

        return meterListener;
    }
}
