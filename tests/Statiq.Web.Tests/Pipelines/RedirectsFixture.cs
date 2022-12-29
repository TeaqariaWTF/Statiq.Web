﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Statiq.App;
using Statiq.Common;
using Statiq.Testing;
using Statiq.Web.Pipelines;

namespace Statiq.Web.Tests.Pipelines
{
    [TestFixture]
    public class RedirectsFixture : BaseFixture
    {
        public class ExecuteTests : RedirectsFixture
        {
            [Test]
            public async Task GeneratesClientRedirects()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory.CreateWeb(Array.Empty<string>());
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/a/b/c.md",
                        @"RedirectFrom: x/y
---
Foo"
                    },
                    {
                        "/input/d/e.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("x/y.html");
                (await document.GetContentStringAsync()).ShouldContain(@"<meta http-equiv=""refresh"" content=""0;url='/a/b/c'"" />");
            }

            [Test]
            public async Task GeneratesNetlifyRedirects()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true)
                    .AddSetting(WebKeys.MetaRefreshRedirects, false);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/a/b/c.md",
                        @"RedirectFrom: x/y
---
Foo"
                    },
                    {
                        "/input/d/e.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                    @"# Automatic redirects generated by Statiq
/x/y /a/b/c",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task ShouldOutputExistingNetlifyFile()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/_redirects",
                        "foobar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe("foobar");
            }

            [Test]
            public async Task ShouldCombineWithExistingNetlifyFile()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true)
                    .AddSetting(WebKeys.MetaRefreshRedirects, false);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/a/b/c.md",
                        @"RedirectFrom: x/y
---
Foo"
                    },
                    {
                        "/input/d/e.md",
                        "Bar"
                    },
                    {
                        "/input/_redirects",
                        "foobar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                @"foobar

# Automatic redirects generated by Statiq
/x/y /a/b/c",
                StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task ShouldNotOutputNetlifyFileIfNoRedirects()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/a/b/c.md",
                        "Foo"
                    },
                    {
                        "/input/d/e.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                result.Outputs[nameof(Redirects)][Phase.Output].ShouldBeEmpty();
            }

            [Test]
            public async Task ShouldGenerateNetlifyPrefixRedirectsByDefault()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/^.a/c.md",
                        @"Foo"
                    },
                    {
                        "/input/d/e.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                    @"# Prefix redirects generated by Statiq
/.a /^.a",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task ShouldNotGenerateNetlifyPrefixRedirects()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true)
                    .AddSetting(WebKeys.NetlifyPrefixRedirects, false);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/^.a/c.md",
                        @"Foo"
                    },
                    {
                        "/input/d/e.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                result.Outputs[nameof(Redirects)][Phase.Output].ShouldBeEmpty();
            }

            [Test]
            public async Task ShouldRedirectedNestedPrefixFolder()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/a/b/^.c/d.md",
                        @"Foo"
                    },
                    {
                        "/input/x/y.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                    @"# Prefix redirects generated by Statiq
/a/b/.c /a/b/^.c",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task ShouldRedirectedNestedPrefixFolderWithNestedFile()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/a/b/^.c/d/e/f.md",
                        @"Foo"
                    },
                    {
                        "/input/x/y.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                    @"# Prefix redirects generated by Statiq
/a/b/.c /a/b/^.c",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task ShouldGenerateSinglePrefixRedirectForMultipleFiles()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/a/b/^.c/d/e/f.md",
                        @"Foo"
                    },
                    {
                        "/input/a/b/^.c/d/g.md",
                        @"Foo"
                    },
                    {
                        "/input/x/y.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                    @"# Prefix redirects generated by Statiq
/a/b/.c /a/b/^.c",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task ShouldOnlyGenerateParentPrefixRedirect()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/a/b/^.c/d/^.e.md",
                        @"Foo"
                    },
                    {
                        "/input/x/y.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                    @"# Prefix redirects generated by Statiq
/a/b/.c /a/b/^.c",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task ShouldUseAlternateRedirectPrefix()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true)
                    .AddSetting(WebKeys.NetlifyRedirectPrefix, "foo");
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/foo.a/c.md",
                        @"Foo"
                    },
                    {
                        "/input/d/e.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                    @"# Prefix redirects generated by Statiq
/.a /foo.a",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task ShouldGeneratePrefixRedirectForFile()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/^.a.md",
                        @"Foo"
                    },
                    {
                        "/input/x/y.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                    @"# Prefix redirects generated by Statiq
/.a /^.a",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task ShouldGeneratePrefixRedirectForNestedFile()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/fizz/^.a.md",
                        @"Foo"
                    },
                    {
                        "/input/x/y.md",
                        "Bar"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                    @"# Prefix redirects generated by Statiq
/fizz/.a /fizz/^.a",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task ShouldCombineAllNetlifyRedirects()
            {
                // Given
                App.Bootstrapper bootstrapper = App.Bootstrapper.Factory
                    .CreateWeb(Array.Empty<string>())
                    .AddSetting(WebKeys.NetlifyRedirects, true)
                    .AddSetting(WebKeys.MetaRefreshRedirects, false);
                TestFileProvider fileProvider = new TestFileProvider
                {
                    {
                        "/input/a/b/c.md",
                        @"RedirectFrom: x/y
---
Foo"
                    },
                    {
                        "/input/d/e.md",
                        "Bar"
                    },
                    {
                        "/input/_redirects",
                        "foobar"
                    },
                    {
                        "/input/a/b/^.c/d/e/f.md",
                        @"Foo"
                    },
                    {
                        "/input/a/b/^.c/d/g.md",
                        @"Foo"
                    }
                };

                // When
                BootstrapperTestResult result = await bootstrapper.RunTestAsync(fileProvider);

                // Then
                result.ExitCode.ShouldBe((int)ExitCode.Normal);
                IDocument document = result.Outputs[nameof(Redirects)][Phase.Output].ShouldHaveSingleItem();
                document.Destination.ShouldBe("_redirects");
                (await document.GetContentStringAsync()).ShouldBe(
                    @"foobar

# Prefix redirects generated by Statiq
/a/b/.c /a/b/^.c

# Automatic redirects generated by Statiq
/x/y /a/b/c",
                    StringCompareShould.IgnoreLineEndings);
            }
        }
    }
}