using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestGitClient;

namespace UnitTestProject1
{
    [TestClass]
    public class FunctionTests
    {     
        [TestMethod]
        public void TestMethodIdentical()
        {         
            var from = TreeHelper.SyntaxTreeFromString(" public void Print(){Console.WriteLine(\"HalloWelt\");}");
            var to = TreeHelper.SyntaxTreeFromString(" public void Print(){Console.WriteLine(\"HalloWelt\");}"); 
            Assert.AreEqual(1, to.childs.Count);    // to and from should be compilation unit
            Assert.AreEqual(1, from.childs.Count);

            var foo = TreeComparison.FindBelongingThing(from.childs[0], to.childs[0]);
            Assert.IsNotNull(foo);
            Assert.IsFalse(foo.wasModified);
            Assert.AreEqual(ModificationKind.noModification, foo.howModified);

        }

        [TestMethod]
        public void TestMethodContentChanged()
        {
            var from = TreeHelper.SyntaxTreeFromString(" public void Print(){Console.WriteLine(\"HalloWelt\");}"); ;
            var to = TreeHelper.SyntaxTreeFromString(" public void Print(){Console.WriteLine(\"Hallo Welt\");}"); ;

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
        public void TestMethodRenamed()
        {
            var from = TreeHelper.SyntaxTreeFromString(" public void Print(){Console.WriteLine(\"HalloWelt\");}"); ;
            var to = TreeHelper.SyntaxTreeFromString(" public void PrintHelloWorld(){Console.WriteLine(\"HalloWelt\");}"); ;

            Assert.AreEqual(1, to.childs.Count);    // to and from should be compilation unit
            Assert.AreEqual(1, from.childs.Count);

            var foo = TreeComparison.FindBelongingThing(from.childs[0], to.childs[0]);
            // gues that the namespace was renamed
            Assert.IsNotNull(foo);
            Assert.IsNotNull(foo.treeNode);
            Assert.IsTrue(foo.wasModified);
            Assert.AreEqual(ModificationKind.nameChanged, foo.howModified);            
        }
    }
}
