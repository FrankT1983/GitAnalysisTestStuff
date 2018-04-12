using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestGitClient;

namespace UnitTestProject1
{
    [TestClass]
    public class ClassTests
    {
        [TestMethod]
        public void TestClassIdentical()
        {
            var from = TreeHelper.SyntaxTreeFromString(" public class Foo1{}");
            var to = TreeHelper.SyntaxTreeFromString(" public class Foo1{}");
            Assert.AreEqual(1, to.childs.Count);    // to and from should be compilation unit
            Assert.AreEqual(1, from.childs.Count);

            var foo = TreeComparison.FindBelongingThing(from.childs[0], to.childs[0]);
            Assert.IsNotNull(foo);
            Assert.IsFalse(foo.wasModified);
            Assert.AreEqual(ModificationKind.noModification, foo.howModified);

        }

        [TestMethod]
        public void TestClassContentChanged()
        {
            var from = TreeHelper.SyntaxTreeFromString(" public class Foo1{}");
            var to = TreeHelper.SyntaxTreeFromString(" public class Foo1{ public void test(){} }");

            Assert.AreEqual(1, to.childs.Count);    // to and from should be compilation unit
            Assert.AreEqual(1, from.childs.Count);

            var foo = TreeComparison.FindBelongingThing(from.childs[0], to.childs[0]);
            // should find the namespace declaration with Foo2 in it
            Assert.IsNotNull(foo);
            Assert.IsNotNull(foo.treeNode);
            Assert.IsTrue(foo.wasModified);
            Assert.AreEqual(ModificationKind.contentChanged, foo.howModified);
        }

        [TestMethod]
        public void TestClassRenamed()
        {
            var from = TreeHelper.SyntaxTreeFromString(" public class Foo1{}");
            var to = TreeHelper.SyntaxTreeFromString(" public class Foo2{}");

            Assert.AreEqual(1, to.childs.Count);    // to and from should be compilation unit
            Assert.AreEqual(1, from.childs.Count);

            var foo = TreeComparison.FindBelongingThing(from.childs[0], to.childs[0]);
            // gues that the namespace was renamed
            Assert.IsNotNull(foo);
            Assert.IsNotNull(foo.treeNode);
            Assert.IsTrue(foo.wasModified);
            Assert.AreEqual(ModificationKind.nameChanged, foo.howModified);
        }

        [TestMethod]
        public void TestMethodAdded()
        {
            var from = TreeHelper.SyntaxTreeFromString(" public void Print(){Console.WriteLine(\"HalloWelt\");}"); ;
            var to = TreeHelper.SyntaxTreeFromString(" public void Print(){Console.WriteLine(\"HalloWelt\");}  public void Print2(){Console.WriteLine(\"blub blub\");}"); 

            Assert.AreEqual(1, from.childs.Count);
            Assert.AreEqual(2, to.childs.Count);    // first level is compilation unit
            

            var foo = TreeComparison.FindBelongingThing(from.childs[0], to.childs);
            // gues that the namespace was renamed
            Assert.IsNotNull(foo);
            Assert.IsNotNull(foo.treeNode);
            Assert.IsFalse(foo.wasModified);
            Assert.AreEqual(ModificationKind.noModification, foo.howModified);
            Assert.IsTrue(foo.treeNode.node.ToFullString().Contains("Hallo"));
        }

        [TestMethod]
        public void TestMethodAdded2()
        {
            var from = TreeHelper.SyntaxTreeFromString(" public void Print(){Console.WriteLine(\"HalloWelt\");}"); ;
            var to = TreeHelper.SyntaxTreeFromString("public void Print2(){Console.WriteLine(\"blub blub\");}  public void Print(){Console.WriteLine(\"HalloWelt\");}  ");

            Assert.AreEqual(1, from.childs.Count);
            Assert.AreEqual(2, to.childs.Count);    // first level is compilation unit


            var foo = TreeComparison.FindBelongingThing(from.childs[0], to.childs);
            // gues that the namespace was renamed
            Assert.IsNotNull(foo);
            Assert.IsNotNull(foo.treeNode);
            Assert.IsFalse(foo.wasModified);
            Assert.AreEqual(ModificationKind.noModification, foo.howModified);
            Assert.IsTrue(foo.treeNode.node.ToFullString().Contains("Hallo"));
        }
    }
}
