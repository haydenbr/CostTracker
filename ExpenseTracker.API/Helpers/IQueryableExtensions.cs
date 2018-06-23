using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Web;

namespace ExpenseTracker.API.Helpers
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string sort)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (sort == null)
            {
                return source;
            }

            var sortOptions = sort.Split(',');
            string sortExpression = "";

            foreach (var sortOption in sortOptions)
            {
                if (sortOption.StartsWith("-"))
                {
                    sortExpression += (sortOption.Remove(0, 1) + " descending,");
                }
                else
                {
                    sortExpression += (sortOption + ",");
                }
            }

            if (!string.IsNullOrWhiteSpace(sortExpression))
            {
                string finalSortExpression = sortExpression.Remove(sortExpression.Count() - 1);
                source = source.OrderBy(finalSortExpression);
            }

            return source;
        }
    }
}