using Sitecore;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.Data;
using Sitecore.Data.Comparers;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Sites;
using Sitecore.XA.Foundation.IoC;
using Sitecore.XA.Foundation.LocalDatasources.Services;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.Multisite.Extensions;
using Sitecore.XA.Foundation.Search.ComputedFields;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
      ISet<Item> dataFolders = new HashSet<Item>();
      foreach (Item folder in new[] { ServiceLocator.Current.Resolve<IMultisiteContext>().GetDataItem(item), ServiceLocator.Current.Resolve<ILocalDatasourceService>().GetPageDataItem(item) })
      {
        if (folder != null)
        {
          dataFolders.Add(folder);
        }
      }

      List<Item> items = new List<Item> { item };
      items.AddRange(GetFieldReferences(item, dataFolders));
      items.AddRange(GetLayoutReferences(item, dataFolders));

      int k = 0;
      while (k < items.Count)
      {
        for (; k < items.Count; k++)
        {
          if (ChildrenGroupingTemplateIds.Any(templateId => items[k].Template.DoesTemplateInheritFrom(templateId)))
          {
            items.AddRange(items[k].Children);
          }
          else if (CompositeTemplateIds.Any(templateId => items[k].Template.DoesTemplateInheritFrom(templateId)))
          {
            items.AddRange(GetLayoutReferences(items[k], dataFolders));
          }
        }
      }

      ProviderIndexConfiguration config = ContentSearchManager.GetIndex(indexable).Configuration;
      return items.Distinct(new ItemIdComparer()).SelectMany(i => ExtractTextFields(i, config));
    }

  }
}
