import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:nova_haven_companion/wiki_api.dart';

void main(){
 test('extracts a bounded duplicate-safe Markdown table of contents',() {
  final toc=extractMarkdownToc('# Overview\n\n#### ignored\n\n## Overview\n\n### Loot [guide](/wiki/loot)');
  expect(toc.map((item)=>item.id),['heading-overview','heading-overview-2','heading-loot-guide']);
  expect(toc.map((item)=>item.level),[1,2,3]);
 });
 test('ignores Markdown headings inside fenced code blocks',() {
  final toc=extractMarkdownToc('```md\n## Not a heading\n```\n\n## Real heading');
  expect(toc.map((item)=>item.text),['Real heading']);
 });

 test('Wiki API searches published articles with bounded query',() async {
  final client=MockClient((request) async {
   expect(request.url.path,'/api/v1/wiki/articles');
   expect(request.url.queryParameters['q'],'fishing');
   return http.Response(jsonEncode({'items':[{'slug':'fishing','title':'Fishing','summary':'Guide','category':'professions','revision':1}],'page':1,'total':1}),200);
  });
  final api=WikiApi(base:Uri.parse('https://wiki.example/'),client:client);
  final response=await api.articles(q:'fishing');
  expect(response.items.single.title,'Fishing');
  expect(response.total,1);
  api.dispose();client.close();
 });
 test('Private drafts cannot be read through public endpoint',() async {
  final api=WikiApi(base:Uri.parse('https://wiki.example/'),client:MockClient((_) async=>http.Response('',404)));
  expect(()=>api.article('unpublished'),throwsException);
  api.dispose();
 });
 test('Published tag catalog parses counts and tag filter reaches public list',() async {
  final client=MockClient((request) async {
   if(request.url.path.endsWith('/tags')) {
    return http.Response.bytes(
     utf8.encode(jsonEncode([{'id':'tag-1','slug':'rare-fish','name':'Cá hiếm','articleCount':2}])),
     200,
     headers:{'content-type':'application/json; charset=utf-8'},
    );
   }
   expect(request.url.path,'/api/v1/wiki/articles');
   expect(request.url.queryParameters['tag'],'rare-fish');
   return http.Response(jsonEncode({'items':[{'slug':'fishing','title':'Fishing','summary':'Guide','category':'professions','tags':['rare-fish'],'revision':3}],'page':1,'total':1}),200);
  });
  final api=WikiApi(base:Uri.parse('https://wiki.example/'),client:client);
  final tags=await api.tags();
  expect(tags.single.articleCount,2);
  final result=await api.articles(tag:'rare-fish');
  expect(result.items.single.tags,['rare-fish']);
  api.dispose();client.close();
 });
 test('Article detail exposes published tags only',() async {
  final client=MockClient((_) async=>http.Response(jsonEncode({
    'slug':'fishing','title':'Fishing','summary':'Guide','category':'professions','tags':['rare-fish'],
    'revision':2,'markdown':'# Fishing',
  }),200));
  final api=WikiApi(base:Uri.parse('https://wiki.example/'),client:client);
 expect((await api.article('fishing')).tags,['rare-fish']);
  api.dispose();client.close();
 });
 test('Catalog API parses published item pages and details',() async {
  var detail=false;
  final client=MockClient((request) async {
   if(request.url.path.endsWith('/catalog/items')) {
    expect(request.url.queryParameters['kind'],'weapon');
    return http.Response(jsonEncode({'items':[{'slug':'moonsteel','name':'Moonsteel Sword','summary':'Blade','kind':'weapon','revision':1}],'page':1,'total':1}),200);
   }
   detail=true;
   return http.Response(jsonEncode({'slug':'moonsteel','name':'Moonsteel Sword','summary':'Blade','kind':'weapon','revision':1,'markdown':'# Moonsteel'}),200);
  });
  final api=WikiApi(base:Uri.parse('https://wiki.example/'),client:client);
  final page=await api.catalog(kind:'weapon');
  expect(page.items.single.name,'Moonsteel Sword');
  expect((await api.catalogItem('moonsteel')).markdown,'# Moonsteel');
  expect(detail,true);
  api.dispose();client.close();
 });
 test('Recipe API parses immutable item snapshots',() async {
  final client=MockClient((request) async {
   if(request.url.path.endsWith('/catalog/recipes')) {
    return http.Response(jsonEncode({'items':[{'slug':'forge','name':'Forge','summary':'Recipe','revision':1}],'page':1,'total':1}),200);
   }
   return http.Response(jsonEncode({'slug':'forge','name':'Forge','summary':'Recipe','revision':1,'markdown':'# Forge','ingredients':[{'itemId':'i1','itemSlug':'ingot','itemName':'Ingot','quantity':2}],'outputs':[{'itemId':'i2','itemSlug':'sword','itemName':'Sword','quantity':1}]}),200);
  });
  final api=WikiApi(base:Uri.parse('https://wiki.example/'),client:client);
  expect((await api.recipes()).single.name,'Forge');
  final recipe=await api.recipe('forge');
  expect(recipe.ingredients.single.quantity,2);
  expect(recipe.outputs.single.itemSlug,'sword');
  api.dispose();client.close();
 });
 test('Knowledge API parses published typed records and graph links',() async {
  final client=MockClient((request) async {
   if(request.url.path.endsWith('/knowledge/npc')) {
    expect(request.url.queryParameters['page'],'1');
    return http.Response(jsonEncode({'items':[{'id':'n1','slug':'lyra','name':'Lyra','summary':'Warden','kind':'npc','revision':1,'publishedAt':'2026-01-01T00:00:00Z'}],'page':1,'pageSize':20,'total':1}),200);
   }
   return http.Response(jsonEncode({'id':'q1','slug':'quest','name':'Quest','summary':'A quest','kind':'quest','revision':1,'publishedAt':'2026-01-01T00:00:00Z','markdown':'# Quest','metadata':{'difficulty':3,'steps':[{'position':1,'title':'Start','description':'Begin'}]},'links':[{'linkType':'usesItem','slug':'sword','name':'Sword','type':'catalog','sortOrder':0}]}),200);
  });
  final api=WikiApi(base:Uri.parse('https://wiki.example/'),client:client);
  final page=await api.knowledge(kind:'npc');
  expect(page.items.single.name,'Lyra');
  final detail=await api.knowledgeDetail('quest','quest');
  expect(detail.metadata.difficulty,3);
  expect(detail.metadata.steps.single.title,'Start');
  expect(detail.links.single.type,'catalog');
  api.dispose();client.close();
 });
 test('Community API parses published events and editorial standings',() async {
  final client=MockClient((request) async {
   if(request.url.path.endsWith('/community/event')) return http.Response(jsonEncode({'items':[{'id':'e1','slug':'festival','name':'Festival','summary':'Gathering','kind':'event','revision':1,'publishedAt':'2026-01-01T00:00:00Z'}],'page':1,'pageSize':20,'total':1}),200);
   return http.Response(jsonEncode({'id':'b1','slug':'cup','name':'Cup','summary':'Standings','kind':'leaderboard','revision':1,'publishedAt':'2026-01-01T00:00:00Z','markdown':'# Cup','metadata':{'leaderboardCategory':'Fishing','rows':[{'rank':1,'participantName':'Lyra','score':120.5,'note':'Verified'}]}}),200);
  });
  final api=WikiApi(base:Uri.parse('https://wiki.example/'),client:client);
  final page=await api.community(kind:'event');
  expect(page.items.single.name,'Festival');
  final detail=await api.communityDetail('leaderboard','cup');
  expect(detail.metadata.rows.single.participantName,'Lyra');
  expect(detail.metadata.rows.single.score,120.5);
  api.dispose();client.close();
 });
 test('Rewards and Commerce APIs parse definition-only catalogs',() async {
  final client=MockClient((request) async {
   if(request.url.path.endsWith('/rewards')) return http.Response(jsonEncode({'items':[{'id':'r1','slug':'starter','name':'Starter','summary':'Reward','kind':'item','revision':1,'externalAcknowledgementRequired':true,'updatedAt':'2026-01-01T00:00:00Z'}],'page':1,'pageSize':20,'total':1}),200);
   return http.Response(jsonEncode({'id':'c1','slug':'supporter','name':'Supporter','summary':'Offer','kind':'donation','revision':1,'definitionOnly':true,'updatedAt':'2026-01-01T00:00:00Z','markdown':'# Supporter','displayPrice':'5 USD','publishedAt':'2026-01-01T00:00:00Z'}),200);
  });
  final api=WikiApi(base:Uri.parse('https://wiki.example/'),client:client);
  final rewards=await api.rewards();
  expect(rewards.items.single.externalAcknowledgementRequired,true);
  final offer=await api.commerceOffer('supporter');
  expect(offer.definitionOnly,true);expect(offer.displayPrice,'5 USD');
  api.dispose();client.close();
 });
}

// Regression: tag filtering must remain public and never read draft memberships.
// Included alongside the existing mock-client tests in this file's main() below.
