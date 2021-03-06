﻿using System.Collections.Generic;
using System.Linq;

namespace LobitaDownloader
{
    public static class IndexUtils
    {
        public static HashSet<string> GetTopSeries(ref IDictionary<string, int> tagOccurrences, int seriesLimit)
        {
            HashSet<string> topSeries = new HashSet<string>();

            if (tagOccurrences.Count > 0)
            {
                int maxSeries = tagOccurrences.Count > seriesLimit ? seriesLimit : tagOccurrences.Count;
                string seriesWithMostTags = tagOccurrences.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                topSeries = new HashSet<string>();

                for (int n = 0; n < maxSeries; n++)
                {
                    topSeries.Add(seriesWithMostTags);
                    tagOccurrences.Remove(seriesWithMostTags);

                    if (tagOccurrences.Count > 0)
                    {
                        seriesWithMostTags = tagOccurrences.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                    }
                }
            }

            return topSeries;
        }
    }
}
