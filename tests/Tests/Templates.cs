using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Mandrill.Model;
using Mandrill.Model;
using NUnit.Framework;

namespace Tests
{
    [Category("templates")]
    internal class Templates : IntegrationTest
    {
        protected HashSet<string> TemplatesToCleanup;

        protected string AddToBeDeleted(string templateName)
        {
            TemplatesToCleanup.Add(templateName);
            return templateName;
        }

        [TestFixtureSetUp]
        public override void SetUp()
        {
            base.SetUp();
            TemplatesToCleanup = new HashSet<string>();
        }

        [TestFixtureTearDown]
        public override void TearDown()
        {
            foreach (var templateName in TemplatesToCleanup)
            {
                var result = Api.Templates.DeleteAsync(templateName).Result;
                result.Should().NotBeNull();
            }
            TemplatesToCleanup = null;
            base.TearDown();
        }

        [Category("templates/add.json")]
        internal class Add : Templates
        {
            [Test]
            public async void Can_add_template()
            {
                var name = AddToBeDeleted(Guid.NewGuid().ToString());
                var result = await Api.Templates.AddAsync(name, TemplateContent.Code, TemplateContent.Text, false);

                result.Name.Should().Be(name);
                result.Code.Should().Be(TemplateContent.Code);
                result.Slug.Should().Be(name);
                result.Text.Should().Be(TemplateContent.Text);
            }
        }

        [Category("templates/list.json")]
        internal class List : Templates
        {
            [Test]
            public async void Can_list_all_templates()
            {
                var testLabel = Guid.NewGuid().ToString("N");
                var templates = Enumerable.Range(1, 10).Select(i => AddToBeDeleted(Guid.NewGuid().ToString())).ToArray();
                foreach (var template in templates)
                {
                    await Api.Templates.AddAsync(template, TemplateContent.Code, TemplateContent.Text, false, labels: new[] {testLabel});
                }

                var results = await Api.Templates.List(testLabel);

                results.Count.Should().BeGreaterOrEqualTo(10);
                results.Where(info => templates.Contains(info.Name)).Should().HaveCount(10);
            }

            [Test]
            public async void Can_list_templates()
            {
                var testLabel = Guid.NewGuid().ToString("N");
                var templates = Enumerable.Range(1, 10).Select(i => AddToBeDeleted(Guid.NewGuid().ToString())).ToArray();
                foreach (var template in templates)
                {
                    await Api.Templates.AddAsync(template, TemplateContent.Code, TemplateContent.Text, false, labels: new[] {testLabel});
                }

                var results = await Api.Templates.List(testLabel);

                results.Should().HaveCount(10);
                results.All(x =>
                {
                    x.Labels.Should().NotBeNullOrEmpty();
                    x.Labels[0].Should().Be(testLabel);
                    return true;
                }).Should().BeTrue();
            }
        }

        [Category("templates/render.json")]
        internal class Render : Templates
        {
            [Test]
            public async void Can_render()
            {
                var name = AddToBeDeleted(Guid.NewGuid().ToString());
                await Api.Templates.AddAsync(name, TemplateContent.Code, TemplateContent.Text, false);

                var templateContent = new List<MandrillTemplateContent>
                {
                    new MandrillTemplateContent {Name = "footer", Content = "this is my footer"}
                };
                var mergeVars = new List<MandrillMergeVar>
                {
                    new MandrillMergeVar {Name = "fname", Content = "Joe"},
                    new MandrillMergeVar {Name = "ORDERDATE", Content = "11/28/2014"},
                    new MandrillMergeVar {Name = "INVOICEDETAILS", Content = "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod"}
                };
                var result = await Api.Templates.RenderAsync(name, templateContent, mergeVars);

                result.Html.Should().NotBeNullOrWhiteSpace();
                result.Html.Should().Contain("Joe");
                result.Html.Should().Contain("11/28/2014");
                result.Html.Should().Contain("Lorem ipsum dolor sit amet");
            }
        }
    }
}