using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hello
{
    public record Sentiment
    {
        public double AveragePositive { get; set; }
        public double AverageNegative { get; set; }
        public double AverageNeutral { get; set; }
    }


    public class SentimentHdr
    {
        public string SpeakerId { get; set; }
        public List<Sentiments> Sentiment { get; set; }
    }
    public class Sentiments
    {
        public string Key { get; set; }
        public double Value { get; set; }
    }
}
