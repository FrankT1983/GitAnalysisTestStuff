using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestGitClient;

namespace UnitTestProject1
{
    [TestClass]
    public class NamespaceTests
    {      
        [TestMethod]
        public void TestNamespaceIdentical()
        {
            var from = TreeHelper.SyntaxTreeFromString(" namespace test{  public class Foo1{}}");
            var to = TreeHelper.SyntaxTreeFromString(" namespace test{  public class Foo1{}}");

            var foo = TreeComparison.FindBelongingThing(from, to);
            Assert.IsNotNull(foo);
            Assert.IsFalse(foo.wasModified);
            Assert.AreEqual(ModificationKind.noModification,foo.howModified);

        }
               
        [TestMethod]
        public void TestNamespaceContentChanged()
        {
            var from = TreeHelper.SyntaxTreeFromString(" namespace test{  public class Foo1{}}");
            var to = TreeHelper.SyntaxTreeFromString(" namespace test{  public class Foo2{}}");

            Assert.AreEqual(1, to.childs.Count);    // to and from should be compilation unit
            Assert.AreEqual(1, from.childs.Count);

            var foo = TreeComparison.FindBelongingThing(from.childs[0], to.childs[0]);
            // should find the namespace declaration with Foo2 in it
            Assert.IsNotNull(foo);
            Assert.IsNotNull(foo.treeNode);
            Assert.IsTrue(foo.wasModified);
            Assert.AreEqual(ModificationKind.contentChanged, foo.howModified);
            Assert.IsTrue(foo.treeNode.node.ToFullString().Contains("Foo2"));
        }

        [TestMethod]
        public void TestNamespaceRenamed()
        {
            var from = TreeHelper.SyntaxTreeFromString(" namespace test{  public class Foo1{}}");
            var to = TreeHelper.SyntaxTreeFromString(" namespace test2{  public class Foo1{}}");

            Assert.AreEqual(1, to.childs.Count);    // to and from should be compilation unit
            Assert.AreEqual(1, from.childs.Count);

            var foo = TreeComparison.FindBelongingThing(from.childs[0], to.childs[0]);
            // gues that the namespace was renamed
            Assert.IsNotNull(foo);
            Assert.IsNotNull(foo.treeNode);
            Assert.IsTrue(foo.wasModified);
            Assert.AreEqual(ModificationKind.nameChanged, foo.howModified);
            Assert.IsTrue(foo.treeNode.node.ToFullString().Contains("Foo1"));
        }
    }
}
