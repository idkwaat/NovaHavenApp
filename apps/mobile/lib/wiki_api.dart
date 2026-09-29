import 'dart:convert';
import 'package:http/http.dart' as http;

class WikiCategory {
  const WikiCategory({required this.slug, required this.name, required this.articleCount});
  final String slug;
  final String name;
  final int articleCount;

  factory WikiCategory.fromJson(Map<String, dynamic> json) => WikiCategory(
    slug: json['slug'] as String,
    name: json['name'] as String,
    articleCount: json['articleCount'] as int,
  );
}

class WikiTag {
  const WikiTag({required this.id,required this.slug,required this.name,required this.articleCount});
  final String id;
  final String slug;
  final String name;
  final int articleCount;
  factory WikiTag.fromJson(Map<String,dynamic> json)=>WikiTag(
    id:json['id'] as String,slug:json['slug'] as String,
    name:json['name'] as String,articleCount:json['articleCount'] as int,
  );
}

class WikiArticleSummary {
  const WikiArticleSummary({required this.slug, required this.title, required this.summary, required this.category, required this.revision,required this.tags});
  final String slug;
  final String title;
  final String summary;
  final String category;
  final int revision;
  final List<String> tags;
  factory WikiArticleSummary.fromJson(Map<String, dynamic> json) => WikiArticleSummary(
    slug: json['slug'] as String,
    title: json['title'] as String,
    summary: json['summary'] as String,
    category: json['category'] as String,
    revision: json['revision'] as int,
    tags:(json['tags'] as List<dynamic>? ?? []).cast<String>(),
  );
}

class WikiArticleDetail extends WikiArticleSummary {
  const WikiArticleDetail({required super.slug,required super.title,required super.summary,
    required super.category,required super.revision,required super.tags,required this.markdown,required this.related});
  final String markdown;
  final List<WikiArticleSummary> related;
  factory WikiArticleDetail.fromJson(Map<String,dynamic> json)=>WikiArticleDetail(
    slug:json['slug'] as String,title:json['title'] as String,
    summary:json['summary'] as String,category:json['category'] as String,
    revision:json['revision'] as int,
    tags:(json['tags'] as List<dynamic>? ?? []).cast<String>(),
    markdown:json['markdown'] as String,
    related:(json['related'] as List<dynamic>? ?? [])
        .map((item)=>WikiArticleSummary.fromJson(item as Map<String,dynamic>)).toList(),
  );
}

class MarkdownHeading {
  const MarkdownHeading({required this.id,required this.text,required this.level});
  final String id;
  final String text;
  final int level;
}

List<MarkdownHeading> extractMarkdownToc(String markdown) {
  final seen=<String,int>{};
  final headings=<MarkdownHeading>[];
  final pattern=RegExp(r'^ {0,3}(#{1,3})\s+(.+?)\s*#*\s*$');
  String? fenceChar;
  for(final line in markdown.split(RegExp(r'\r?\n'))){
    final fence=RegExp(r'^ {0,3}(`{3,}|~{3,})').firstMatch(line);
    if(fence!=null){
      final nextFence=fence.group(1)![0];
      if(fenceChar==null){fenceChar=nextFence;}
      else if(fenceChar==nextFence){fenceChar=null;}
      continue;
    }
    if(fenceChar!=null)continue;
    final match=pattern.firstMatch(line);
    if(match==null)continue;
    var text=match.group(2)!
      .replaceAllMapped(RegExp(r'\[([^\]]+)\]\([^)]*\)'),(m)=>m.group(1)!)
      .replaceAllMapped(RegExp(r'`([^`]+)`'),(m)=>m.group(1)!)
      .replaceAll(RegExp(r'[\*_~]'),'')
      .replaceAll(RegExp(r'<[^>]*>'),'').trim();
    if(text.isEmpty)continue;
    var base=text.toLowerCase().replaceAll(RegExp(r'[^a-z0-9]+'),'-')
      .replaceAll(RegExp(r'^-+|-+$'),'');
    if(base.isEmpty)base='section';
    final count=(seen[base]??0)+1;
    seen[base]=count;
    headings.add(MarkdownHeading(id:'heading-$base${count>1?'-$count':''}',text:text,level:match.group(1)!.length));
  }
  return headings;
}

class WikiPage {
  const WikiPage({required this.items, required this.page, required this.total});
  final List<WikiArticleSummary> items;
  final int page;
  final int total;
  factory WikiPage.fromJson(Map<String,dynamic> json)=>WikiPage(
    items:(json['items'] as List<dynamic>)
        .map((item)=>WikiArticleSummary.fromJson(item as Map<String,dynamic>)).toList(),
    page:json['page'] as int,total:json['total'] as int,
  );
}

class NewsSummary {
  const NewsSummary({required this.slug, required this.title, required this.summary, required this.publishedAt});
  final String slug;
  final String title;
  final String summary;
  final DateTime publishedAt;
  factory NewsSummary.fromJson(Map<String, dynamic> json) => NewsSummary(
    slug: json['slug'] as String,
    title: json['title'] as String,
    summary: json['summary'] as String,
    publishedAt: DateTime.parse(json['publishedAt'] as String).toUtc(),
  );
}

class NewsDetail extends NewsSummary {
  const NewsDetail({required super.slug, required super.title, required super.summary,
    required super.publishedAt, required this.markdown});
  final String markdown;
  factory NewsDetail.fromJson(Map<String, dynamic> json) => NewsDetail(
    slug: json['slug'] as String,
    title: json['title'] as String,
    summary: json['summary'] as String,
    publishedAt: DateTime.parse(json['publishedAt'] as String).toUtc(),
    markdown: json['markdown'] as String,
  );
}

class CatalogSummary {
  const CatalogSummary({required this.slug,required this.name,required this.summary,required this.kind,required this.revision});
  final String slug;
  final String name;
  final String summary;
  final String kind;
  final int revision;
  factory CatalogSummary.fromJson(Map<String,dynamic> json)=>CatalogSummary(
    slug:json['slug'] as String,name:json['name'] as String,
    summary:json['summary'] as String,kind:json['kind'] as String,
    revision:json['revision'] as int,
  );
}

class CatalogDetail extends CatalogSummary {
  const CatalogDetail({required super.slug,required super.name,required super.summary,
    required super.kind,required super.revision,required this.markdown});
  final String markdown;
  factory CatalogDetail.fromJson(Map<String,dynamic> json)=>CatalogDetail(
    slug:json['slug'] as String,name:json['name'] as String,
    summary:json['summary'] as String,kind:json['kind'] as String,
    revision:json['revision'] as int,markdown:json['markdown'] as String,
  );
}

class CatalogPage {
  const CatalogPage({required this.items,required this.page,required this.total});
  final List<CatalogSummary> items;
  final int page;
  final int total;
  factory CatalogPage.fromJson(Map<String,dynamic> json)=>CatalogPage(
    items:(json['items'] as List<dynamic>)
        .map((item)=>CatalogSummary.fromJson(item as Map<String,dynamic>)).toList(),
    page:json['page'] as int,total:json['total'] as int,
  );
}

class RecipeComponent {
  const RecipeComponent({required this.itemId,required this.itemSlug,required this.itemName,required this.quantity});
  final String itemId;
  final String itemSlug;
  final String itemName;
  final int quantity;
  factory RecipeComponent.fromJson(Map<String,dynamic> json)=>RecipeComponent(
    itemId:json['itemId'] as String,itemSlug:json['itemSlug'] as String,
    itemName:json['itemName'] as String,quantity:json['quantity'] as int,
  );
}

class RecipeSummary {
  const RecipeSummary({required this.slug,required this.name,required this.summary,required this.revision});
  final String slug;
  final String name;
  final String summary;
  final int revision;
  factory RecipeSummary.fromJson(Map<String,dynamic> json)=>RecipeSummary(
    slug:json['slug'] as String,name:json['name'] as String,
    summary:json['summary'] as String,revision:json['revision'] as int,
  );
}

class RecipeDetail extends RecipeSummary {
  const RecipeDetail({required super.slug,required super.name,required super.summary,
    required super.revision,required this.markdown,required this.ingredients,required this.outputs});
  final String markdown;
  final List<RecipeComponent> ingredients;
  final List<RecipeComponent> outputs;
  factory RecipeDetail.fromJson(Map<String,dynamic> json)=>RecipeDetail(
    slug:json['slug'] as String,name:json['name'] as String,
    summary:json['summary'] as String,revision:json['revision'] as int,
    markdown:json['markdown'] as String,
    ingredients:(json['ingredients'] as List<dynamic>? ?? []).map((item)=>RecipeComponent.fromJson(item as Map<String,dynamic>)).toList(),
    outputs:(json['outputs'] as List<dynamic>? ?? []).map((item)=>RecipeComponent.fromJson(item as Map<String,dynamic>)).toList(),
  );
}

class KnowledgeSummary {
  const KnowledgeSummary({required this.id,required this.slug,required this.name,required this.summary,required this.kind,required this.revision,required this.publishedAt});
  final String id;
  final String slug;
  final String name;
  final String summary;
  final String kind;
  final int revision;
  final DateTime publishedAt;
  factory KnowledgeSummary.fromJson(Map<String,dynamic> json)=>KnowledgeSummary(
    id:json['id'] as String,slug:json['slug'] as String,name:json['name'] as String,
    summary:json['summary'] as String,kind:json['kind'] as String,revision:json['revision'] as int,
    publishedAt:DateTime.parse(json['publishedAt'] as String).toUtc(),
  );
}

class KnowledgeTarget {
  const KnowledgeTarget({required this.slug,required this.name,required this.kind});
  final String slug;
  final String name;
  final String kind;
  factory KnowledgeTarget.fromJson(Map<String,dynamic> json)=>KnowledgeTarget(slug:json['slug'] as String,name:json['name'] as String,kind:json['kind'] as String);
}

class KnowledgeStep {
  const KnowledgeStep({required this.position,required this.title,required this.description});
  final int position;
  final String title;
  final String description;
  factory KnowledgeStep.fromJson(Map<String,dynamic> json)=>KnowledgeStep(position:json['position'] as int,title:json['title'] as String,description:json['description'] as String);
}

class KnowledgeMetadata {
  const KnowledgeMetadata({this.role,this.portraitUrl,this.location,this.difficulty,this.giver,this.rewardDescription,this.steps=const [],this.region,this.locationType,this.latitude,this.longitude,this.mapImageUrl,this.startsAt,this.endsAt,this.theme,this.eventDescription});
  final String? role;
  final String? portraitUrl;
  final KnowledgeTarget? location;
  final int? difficulty;
  final KnowledgeTarget? giver;
  final String? rewardDescription;
  final List<KnowledgeStep> steps;
  final String? region;
  final String? locationType;
  final double? latitude;
  final double? longitude;
  final String? mapImageUrl;
  final DateTime? startsAt;
  final DateTime? endsAt;
  final String? theme;
  final String? eventDescription;
  factory KnowledgeMetadata.fromJson(Map<String,dynamic> json)=>KnowledgeMetadata(
    role:json['role'] as String?,portraitUrl:json['portraitUrl'] as String?,
    location:json['location'] is Map<String,dynamic>? KnowledgeTarget.fromJson(json['location'] as Map<String,dynamic>):null,
    difficulty:json['difficulty'] as int?,giver:json['giver'] is Map<String,dynamic>? KnowledgeTarget.fromJson(json['giver'] as Map<String,dynamic>):null,
    rewardDescription:json['rewardDescription'] as String?,
    steps:(json['steps'] as List<dynamic>? ?? []).map((item)=>KnowledgeStep.fromJson(item as Map<String,dynamic>)).toList(),
    region:json['region'] as String?,locationType:json['locationType'] as String?,
    latitude:(json['latitude'] as num?)?.toDouble(),longitude:(json['longitude'] as num?)?.toDouble(),mapImageUrl:json['mapImageUrl'] as String?,
    startsAt:json['startsAt'] is String? DateTime.parse(json['startsAt'] as String).toUtc():null,
    endsAt:json['endsAt'] is String? DateTime.parse(json['endsAt'] as String).toUtc():null,
    theme:json['theme'] as String?,eventDescription:json['eventDescription'] as String?,
  );
}

class KnowledgeLink {
  const KnowledgeLink({required this.linkType,required this.slug,required this.name,required this.type,required this.sortOrder});
  final String linkType;
  final String slug;
  final String name;
  final String type;
  final int sortOrder;
  factory KnowledgeLink.fromJson(Map<String,dynamic> json)=>KnowledgeLink(linkType:json['linkType'] as String,slug:json['slug'] as String,name:json['name'] as String,type:json['type'] as String,sortOrder:json['sortOrder'] as int);
}

class KnowledgeDetail extends KnowledgeSummary {
  const KnowledgeDetail({required super.id,required super.slug,required super.name,required super.summary,required super.kind,required super.revision,required super.publishedAt,required this.markdown,required this.metadata,required this.links});
  final String markdown;
  final KnowledgeMetadata metadata;
  final List<KnowledgeLink> links;
  factory KnowledgeDetail.fromJson(Map<String,dynamic> json)=>KnowledgeDetail(
    id:json['id'] as String,slug:json['slug'] as String,name:json['name'] as String,summary:json['summary'] as String,
    kind:json['kind'] as String,revision:json['revision'] as int,publishedAt:DateTime.parse(json['publishedAt'] as String).toUtc(),
    markdown:json['markdown'] as String,metadata:KnowledgeMetadata.fromJson(json['metadata'] as Map<String,dynamic>? ?? {}),
    links:(json['links'] as List<dynamic>? ?? []).map((item)=>KnowledgeLink.fromJson(item as Map<String,dynamic>)).toList(),
  );
}

class KnowledgePage {
  const KnowledgePage({required this.items,required this.page,required this.total});
  final List<KnowledgeSummary> items;
  final int page;
  final int total;
  factory KnowledgePage.fromJson(Map<String,dynamic> json)=>KnowledgePage(
    items:(json['items'] as List<dynamic>).map((item)=>KnowledgeSummary.fromJson(item as Map<String,dynamic>)).toList(),
    page:json['page'] as int,total:json['total'] as int,
  );
}

class CommunitySummary {
  const CommunitySummary({required this.id,required this.slug,required this.name,required this.summary,required this.kind,required this.revision,required this.publishedAt});
  final String id;
  final String slug;
  final String name;
  final String summary;
  final String kind;
  final int revision;
  final DateTime publishedAt;
  factory CommunitySummary.fromJson(Map<String,dynamic> json)=>CommunitySummary(id:json['id'] as String,slug:json['slug'] as String,name:json['name'] as String,summary:json['summary'] as String,kind:json['kind'] as String,revision:json['revision'] as int,publishedAt:DateTime.parse(json['publishedAt'] as String).toUtc());
}

class CommunityRow {
  const CommunityRow({required this.rank,required this.participantName,required this.score,required this.note});
  final int rank;
  final String participantName;
  final double score;
  final String note;
  factory CommunityRow.fromJson(Map<String,dynamic> json)=>CommunityRow(rank:json['rank'] as int,participantName:json['participantName'] as String,score:(json['score'] as num).toDouble(),note:json['note'] as String);
}

class CommunityMetadata {
  const CommunityMetadata({this.startsAt,this.endsAt,this.capacity,this.registrationOpen,this.motto,this.discordUrl,this.handle,this.bio,this.avatarUrl,this.ownerDisplayName,this.galleryMarkdown,this.leaderboardCategory,this.rows=const []});
  final DateTime? startsAt;
  final DateTime? endsAt;
  final int? capacity;
  final bool? registrationOpen;
  final String? motto;
  final String? discordUrl;
  final String? handle;
  final String? bio;
  final String? avatarUrl;
  final String? ownerDisplayName;
  final String? galleryMarkdown;
  final String? leaderboardCategory;
  final List<CommunityRow> rows;
  factory CommunityMetadata.fromJson(Map<String,dynamic> json)=>CommunityMetadata(
    startsAt:json['startsAt'] is String?DateTime.parse(json['startsAt'] as String).toUtc():null,endsAt:json['endsAt'] is String?DateTime.parse(json['endsAt'] as String).toUtc():null,
    capacity:json['capacity'] as int?,registrationOpen:json['registrationOpen'] as bool?,motto:json['motto'] as String?,discordUrl:json['discordUrl'] as String?,handle:json['handle'] as String?,bio:json['bio'] as String?,avatarUrl:json['avatarUrl'] as String?,ownerDisplayName:json['ownerDisplayName'] as String?,galleryMarkdown:json['galleryMarkdown'] as String?,leaderboardCategory:json['leaderboardCategory'] as String?,rows:(json['rows'] as List<dynamic>? ?? []).map((item)=>CommunityRow.fromJson(item as Map<String,dynamic>)).toList(),
  );
}

class CommunityDetail extends CommunitySummary {
  const CommunityDetail({required super.id,required super.slug,required super.name,required super.summary,required super.kind,required super.revision,required super.publishedAt,required this.markdown,required this.metadata});
  final String markdown;
  final CommunityMetadata metadata;
  factory CommunityDetail.fromJson(Map<String,dynamic> json)=>CommunityDetail(id:json['id'] as String,slug:json['slug'] as String,name:json['name'] as String,summary:json['summary'] as String,kind:json['kind'] as String,revision:json['revision'] as int,publishedAt:DateTime.parse(json['publishedAt'] as String).toUtc(),markdown:json['markdown'] as String,metadata:CommunityMetadata.fromJson(json['metadata'] as Map<String,dynamic>? ?? {}));
}

class CommunityPage {
  const CommunityPage({required this.items,required this.page,required this.total});
  final List<CommunitySummary> items;
  final int page;
  final int total;
  factory CommunityPage.fromJson(Map<String,dynamic> json)=>CommunityPage(items:(json['items'] as List<dynamic>).map((item)=>CommunitySummary.fromJson(item as Map<String,dynamic>)).toList(),page:json['page'] as int,total:json['total'] as int);
}

class RewardSummary {
  const RewardSummary({required this.id,required this.slug,required this.name,required this.summary,required this.kind,required this.revision,required this.externalAcknowledgementRequired,required this.updatedAt});
  final String id,slug,name,summary,kind;
  final int revision;
  final bool externalAcknowledgementRequired;
  final DateTime updatedAt;
  factory RewardSummary.fromJson(Map<String,dynamic> json)=>RewardSummary(id:json['id'] as String,slug:json['slug'] as String,name:json['name'] as String,summary:json['summary'] as String,kind:json['kind'] as String,revision:json['revision'] as int,externalAcknowledgementRequired:json['externalAcknowledgementRequired'] as bool,updatedAt:DateTime.parse(json['updatedAt'] as String).toUtc());
}

class RewardPage {
  const RewardPage({required this.items,required this.page,required this.total});
  final List<RewardSummary> items;
  final int page,total;
  factory RewardPage.fromJson(Map<String,dynamic> json)=>RewardPage(items:(json['items'] as List<dynamic>).map((item)=>RewardSummary.fromJson(item as Map<String,dynamic>)).toList(),page:json['page'] as int,total:json['total'] as int);
}

class RewardDetail extends RewardSummary {
  const RewardDetail({required super.id,required super.slug,required super.name,required super.summary,required super.kind,required super.revision,required super.externalAcknowledgementRequired,required super.updatedAt,required this.markdown,required this.deliveryDescription,required this.publishedAt});
  final String markdown,deliveryDescription;
  final DateTime publishedAt;
  factory RewardDetail.fromJson(Map<String,dynamic> json)=>RewardDetail(id:json['id'] as String,slug:json['slug'] as String,name:json['name'] as String,summary:json['summary'] as String,kind:json['kind'] as String,revision:((json['number']??json['revision']) as num).toInt(),externalAcknowledgementRequired:json['externalAcknowledgementRequired'] as bool,updatedAt:DateTime.parse(json['publishedAt'] as String).toUtc(),markdown:json['markdown'] as String,deliveryDescription:json['deliveryDescription'] as String,publishedAt:DateTime.parse(json['publishedAt'] as String).toUtc());
}

class CommerceSummary {
  const CommerceSummary({required this.id,required this.slug,required this.name,required this.summary,required this.kind,required this.revision,required this.definitionOnly,required this.updatedAt});
  final String id,slug,name,summary,kind;
  final int revision;
  final bool definitionOnly;
  final DateTime updatedAt;
  factory CommerceSummary.fromJson(Map<String,dynamic> json)=>CommerceSummary(id:json['id'] as String,slug:json['slug'] as String,name:json['name'] as String,summary:json['summary'] as String,kind:json['kind'] as String,revision:json['revision'] as int,definitionOnly:json['definitionOnly'] as bool,updatedAt:DateTime.parse(json['updatedAt'] as String).toUtc());
}

class CommercePage {
  const CommercePage({required this.items,required this.page,required this.total});
  final List<CommerceSummary> items;
  final int page,total;
  factory CommercePage.fromJson(Map<String,dynamic> json)=>CommercePage(items:(json['items'] as List<dynamic>).map((item)=>CommerceSummary.fromJson(item as Map<String,dynamic>)).toList(),page:json['page'] as int,total:json['total'] as int);
}

class CommerceDetail extends CommerceSummary {
  const CommerceDetail({required super.id,required super.slug,required super.name,required super.summary,required super.kind,required super.revision,required super.definitionOnly,required super.updatedAt,required this.markdown,required this.displayPrice,required this.publishedAt});
  final String markdown,displayPrice;
  final DateTime publishedAt;
  factory CommerceDetail.fromJson(Map<String,dynamic> json)=>CommerceDetail(id:json['id'] as String,slug:json['slug'] as String,name:json['name'] as String,summary:json['summary'] as String,kind:json['kind'] as String,revision:((json['number']??json['revision']) as num).toInt(),definitionOnly:json['definitionOnly'] as bool,updatedAt:DateTime.parse(json['publishedAt'] as String).toUtc(),markdown:json['markdown'] as String,displayPrice:json['displayPrice'] as String,publishedAt:DateTime.parse(json['publishedAt'] as String).toUtc());
}

class WikiApi {
  WikiApi({required Uri base, http.Client? client}) :
    _base = base, _client = client ?? http.Client(), _ownsClient = client == null;
  final Uri _base;
  final http.Client _client;
  final bool _ownsClient;

  Future<Map<String,dynamic>> _get(String path,[Map<String,String>? query]) async {
    final url = _base.resolve(path).replace(queryParameters: query);
    final response=await _client.get(url,headers:{'Accept':'application/json'})
      .timeout(const Duration(seconds:10));
    if(response.statusCode!=200)throw Exception('Wiki API trả về HTTP ${response.statusCode}');
    final json=jsonDecode(utf8.decode(response.bodyBytes));
    if(json is! Map<String,dynamic>)throw const FormatException('Invalid API response');
    return json;
  }

  Future<List<WikiCategory>> categories() async {
    final url=_base.resolve('api/v1/wiki/categories');
    final response=await _client.get(url,headers:{'Accept':'application/json'})
      .timeout(const Duration(seconds:10));
    if(response.statusCode!=200)throw Exception('Danh mục không khả dụng: HTTP ${response.statusCode}');
    final parsed=jsonDecode(utf8.decode(response.bodyBytes));
    if(parsed is! List<dynamic>)throw const FormatException('Invalid categories');
    return parsed.map((item)=>WikiCategory.fromJson(item as Map<String,dynamic>)).toList();
  }

  Future<List<WikiTag>> tags() async {
    final url=_base.resolve('api/v1/wiki/tags');
    final response=await _client.get(url,headers:{'Accept':'application/json'})
      .timeout(const Duration(seconds:10));
    if(response.statusCode!=200)throw Exception('Tag không khả dụng: HTTP ${response.statusCode}');
    final parsed=jsonDecode(utf8.decode(response.bodyBytes));
    if(parsed is! List<dynamic>)throw const FormatException('Invalid tag catalog');
    return parsed.map((item)=>WikiTag.fromJson(item as Map<String,dynamic>)).toList();
  }

  Future<WikiPage> articles({String q='',String category='',String tag='',int page=1}) async {
    final query={'page':'$page','pageSize':'20'};
    final term=q.trim();
    if(term.isNotEmpty)query['q']=term.length>100?term.substring(0,100):term;
    if(category.isNotEmpty)query['category']=category;
    if(tag.isNotEmpty)query['tag']=tag;
    return WikiPage.fromJson(await _get('api/v1/wiki/articles',query));
  }

  Future<WikiArticleDetail> article(String slug) async => WikiArticleDetail.fromJson(
    await _get('api/v1/wiki/articles/${Uri.encodeComponent(slug)}'));

  Future<List<NewsSummary>> news() async {
    final parsed = await _get('api/v1/news', {'page': '1', 'pageSize': '20'});
    final items = parsed['items'];
    if (items is! List<dynamic>) throw const FormatException('Invalid news response');
    return items.map((item) => NewsSummary.fromJson(item as Map<String, dynamic>)).toList();
  }

  Future<NewsDetail> newsArticle(String slug) async => NewsDetail.fromJson(
    await _get('api/v1/news/${Uri.encodeComponent(slug)}'));

  Future<CatalogPage> catalog({String q='',String kind='',int page=1}) async {
    final query={'page':'$page','pageSize':'20'};
    final term=q.trim();
    if(term.isNotEmpty)query['q']=term.length>100?term.substring(0,100):term;
    if(kind.isNotEmpty)query['kind']=kind;
    return CatalogPage.fromJson(await _get('api/v1/catalog/items',query));
  }

  Future<CatalogDetail> catalogItem(String slug) async => CatalogDetail.fromJson(
    await _get('api/v1/catalog/items/${Uri.encodeComponent(slug)}'));

  Future<List<RecipeSummary>> recipes() async {
    final parsed=await _get('api/v1/catalog/recipes',{'page':'1','pageSize':'50'});
    final items=parsed['items'];
    if(items is! List<dynamic>)throw const FormatException('Invalid recipes response');
    return items.map((item)=>RecipeSummary.fromJson(item as Map<String,dynamic>)).toList();
  }

  Future<RecipeDetail> recipe(String slug) async => RecipeDetail.fromJson(
    await _get('api/v1/catalog/recipes/${Uri.encodeComponent(slug)}'));

  Future<KnowledgePage> knowledge({String kind='',String q='',int page=1}) async {
    final query={'page':'$page','pageSize':'20'};
    final term=q.trim();
    if(term.isNotEmpty)query['q']=term.length>100?term.substring(0,100):term;
    final path=kind.isEmpty?'api/v1/knowledge':'api/v1/knowledge/${Uri.encodeComponent(kind)}';
    return KnowledgePage.fromJson(await _get(path,query));
  }

  Future<KnowledgeDetail> knowledgeDetail(String kind,String slug) async => KnowledgeDetail.fromJson(
    await _get('api/v1/knowledge/${Uri.encodeComponent(kind)}/${Uri.encodeComponent(slug)}'));

  Future<CommunityPage> community({String kind='',String q='',int page=1}) async {
    final query={'page':'$page','pageSize':'20'};final term=q.trim();if(term.isNotEmpty)query['q']=term.length>100?term.substring(0,100):term;
    final path=kind.isEmpty?'api/v1/community':'api/v1/community/${Uri.encodeComponent(kind)}';return CommunityPage.fromJson(await _get(path,query));
  }

  Future<CommunityDetail> communityDetail(String kind,String slug) async => CommunityDetail.fromJson(
    await _get('api/v1/community/${Uri.encodeComponent(kind)}/${Uri.encodeComponent(slug)}'));

  Future<RewardPage> rewards() async => RewardPage.fromJson(await _get('api/v1/rewards',{'page':'1','pageSize':'20'}));
  Future<RewardDetail> reward(String slug) async => RewardDetail.fromJson(await _get('api/v1/rewards/${Uri.encodeComponent(slug)}'));
  Future<CommercePage> commerce() async => CommercePage.fromJson(await _get('api/v1/commerce/offers',{'page':'1','pageSize':'20'}));
  Future<CommerceDetail> commerceOffer(String slug) async => CommerceDetail.fromJson(await _get('api/v1/commerce/offers/${Uri.encodeComponent(slug)}'));

  void dispose(){if(_ownsClient)_client.close();}
}
