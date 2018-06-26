using Microsoft.Extensions.DependencyInjection;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.Data;
using Sitecore.Data.Comparers;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.XA.Foundation.LocalDatasources.Services;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.Multisite.Extensions;
using Sitecore.XA.Foundation.Search.ComputedFields;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Sitecore.Support.XA.Foundation.Search.ComputedFields
{
  public class AggregatedContent : Sitecore.XA.Foundation.Search.ComputedFields.AggregatedContent
  {
    private readonly MediaItemContentExtractor _mediaContentExtractor;
    public AggregatedContent()
    {
      _mediaContentExtractor = new MediaItemContentExtractor();
    }

    public AggregatedContent(XmlNode configurationNode)
    {
      _mediaContentExtractor = new MediaItemContentExtractor(configurationNode);
    }

    public override object ComputeFieldValue(IIndexable indexable)
    {
      Item item = indexable as SitecoreIndexableItem;
      if (item == null)
      {
        return null;
      }
      if (item.Paths.IsMediaItem)
      {
        return _mediaContentExtractor.ComputeFieldValue(indexable);
      }
      if (!item.IsPageItem() && !IsPoi.Verify(item))
      {
        return null;
      }
      ISet<Item> set = new HashSet<Item>();
      Item[] array = new Item[2]
      {
            ServiceLocator.ServiceProvider.GetService<IMultisiteContext>().GetDataItem(item),
            ServiceLocator.ServiceProvider.GetService<ILocalDatasourceService>().GetPageDataItem(item)
      };
      foreach (Item item2 in array)
      {
        if (item2 != null)
        {
          set.Add(item2);
        }
      }
      List<Item> items = new List<Item>
        {
            item
        };
      items.AddRange(GetFieldReferences(item, set));
      items.AddRange(GetLayoutReferences(item, set));
      int j = 0;
      while (j < items.Count)
      {
        for (; j < items.Count; j++)
        {
          if (ChildrenGroupingTemplateIds.Any((ID templateId) => items[j].Template.DoesTemplateInheritFrom(templateId)))
          {
            IEnumerable<Item> unique = GetUnique(items[j].Children, items);
            items.AddRange(unique);
          }
          else if (CompositeTemplateIds.Any((ID templateId) => items[j].Template.DoesTemplateInheritFrom(templateId)))
          {
            IEnumerable<Item> unique2 = GetUnique(GetLayoutReferences(items[j], set), items);
            items.AddRange(unique2);
          }
        }
      }
      ProviderIndexConfiguration config = ContentSearchManager.GetIndex(indexable).Configuration;
      return items.Distinct(new ItemIdComparer()).SelectMany((Item i) => ExtractTextFields(i, config));
    }

  }
}
