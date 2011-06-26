using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Kiss.Linq.Sql;
using Kiss.Linq.Sql.DataBase;
using NUnit.Framework;

namespace Kiss.Linq.Linq2Sql.Test
{
    [TestFixture]
    public class EntryPoint : IDisposable
    {
        public EntryPoint()
        {
            ServiceLocator.Instance.Init(() =>
            {
                ServiceLocator.Instance.AddComponent("fj", typeof(ITypeFinder), typeof(AppDomainTypeFinder));
            }, true);

            ServiceLocator.Instance.AddComponent("System.Data.SQLite", typeof(SqliteDataProvider));

            ServiceLocator.Instance.AddComponent("kiss.repository_1", typeof(IRepository<>), typeof(Repository<>));
            ServiceLocator.Instance.AddComponent("kiss.repository_2", typeof(IRepository<,>), typeof(Repository<,>));

            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["sqlite"];

            bookContext = new SqlQuery<Book>(settings);

            libraryContext = new SqlQuery<Library>(settings);

            shelveContext = new SqlQuery<Shelve>(settings);
            // make sure the db is empty.
            DeleteAll();
        }

        [SetUp]
        public void Setup()
        {
            Library library = new Library { Floor = "1A", Section = "Technology" };
            libraryContext.Add(library);
            libraryContext.SubmitChanges();

            for (int index = 0; index < 2; index++)
            {

                Shelve shelve = new Shelve
                {
                    Column = index,
                    Row = index + 1,
                    ShelveNo = Guid.NewGuid().ToString(),
                    LibradyId = library.Id
                };

                shelveContext.Add(shelve);
                shelves.Add(shelve);

                shelveContext.SubmitChanges();

                Assert.IsTrue(shelves[index].Id > 0);
            }


            Book book1 = new Book();
            book1.Title = "Introducing Microsoft LINQ";
            book1.Author = "Paolo Pialorsi";
            book1.ISBN = "111-000-1";
            book1.ShelveId = shelves[0].Id;
            book1.LastUpdated = DateTime.Parse("2007/1/1");

            bookContext.Add(book1);

            books.Add(book1);

            Book book2 = new Book();
            book2.Title = "Foundations of F# (Expert's Voice in .Net)";
            book2.Author = "Robert Pickering";
            book2.ISBN = "111-000-2";
            book1.ShelveId = shelves[1].Id;
            book2.LastUpdated = DateTime.Parse("2007/5/1");

            bookContext.Add(book2);

            books.Add(book2);

            bookContext.SubmitChanges();

            Assert.IsTrue(books[0].Id > 0);
            Assert.IsTrue(books[1].Id > 0);

        }

        [Test]
        public void TestJoin()
        {
            /// Query provider fires onece
            /// Join = 5 arguments => CreateQuery
            /// Where = 2 arguments => CreateQuery
            /// Expression type fires twice.

            var query = from book in bookContext
                        join shelve in shelveContext on book.ShelveId equals shelve.Id
                        //where shelve.Id == 1
                        select book;

            foreach (Book book in query)
            {

            }
        }

        [Test]
        public void GroupBy()
        {
            var query = from book in bookContext
                        group book by book.Author into author
                        select new { author = author };

            foreach (var book in query)
            {

            }
        }

        //[Test]
        //public void Distinct()
        //{

        //    this.Add();
        //    this.Add();

        //    var query = (from  book in bookContext
        //                 where book.Author == "Don Box"
        //                 select book.Id);

        //    Assert.AreEqual(1, query.Count());
        //}

        [Test]
        public void Like()
        {
            this.Add();

            var query = from book in bookContext
                        where book.Author.Contains("Don Box")
                        select book;

            Assert.AreEqual(1, query.Count());

            Assert.AreEqual(1, (from book in bookContext
                                where book.Author.Contains("Don Box")
                                select book).Count());

            Assert.AreEqual(1, query.Count());

            query = from book in bookContext
                    where book.Author.StartsWith("Don")
                    select book;

            Assert.AreEqual(1, query.Count());

            query = from book in bookContext
                    where book.Author.EndsWith("Box")
                    select book;

            Assert.AreEqual(1, query.Count());

            query = from book in bookContext
                    where book.Author.EndsWith("Don")
                    select book;

            Assert.AreEqual(0, query.Count());

            query = from book in bookContext
                    where book.Author.Contains("Donx")
                    select book;

            Assert.AreEqual(0, query.Count());
        }

        [Test]
        public void Equals()
        {
            this.Add();

            var query = from book in bookContext
                        where book.Author.Equals("Don Box")
                        select book;

            Assert.AreEqual(1, query.Count());

            string author = "Donx";

            query = from book in bookContext
                    where book.Author.Equals(author)
                    select book;

            Assert.AreEqual(0, query.Count());

            KeyValuePair<string, string> p = new KeyValuePair<string, string>("Don Box", "Donx");

            query = from book in bookContext
                    where book.Author.Equals(p.Key)
                    select book;

            Assert.AreEqual(1, query.Count());
        }

        [Test]
        public void TestORExpression()
        {
            this.Add();

            //var query = from book in bookContext
            //            where book.Id == books[2].Id || ((book.Author == "Paolo Pialorsi" && book.ISBN == "100-11-777") || book.Author == "Mehfuz")
            //            select book;

            //var query = from book in bookContext
            //            where (book.Id == books[2].Id || book.Author == "Mehfuz") || (book.Author == "Paolo Pialorsi" && book.ISBN == "100-11-777")
            //            select book;

            var query = from book in bookContext
                        where ((book.Author == "Mehfuz" && book.ISBN == "2") || book.Id == books[2].Id) || (book.Author == "Paolo Pialorsi" && (book.ISBN == "100-11-777" || book.ISBN == "1"))
                        orderby book.Id ascending
                        select book;

            int count = query.Count();

            Assert.AreEqual(1, query.Count());

            Book b = query.Single();

            Assert.AreEqual(b.Author, "Don Box");
        }

        [Test]
        public void TestMultipleWhereExtensionCall()
        {
            Book book1 = new Book();
            book1.Title = "Programming Advanced LINQ";
            book1.Author = "Paolo Pialorsi";
            book1.ISBN = "111-000-1";
            book1.ShelveId = shelves[0].Id;
            book1.LastUpdated = DateTime.Now;

            bookContext.Add(book1);
            bookContext.SubmitChanges();

            var query =
                bookContext.Where(book => book.Id == books[0].Id)
                            .Where(book => book.ISBN == books[0].ISBN)
                            .Where(book => book.Author == books[0].Author)
                            .Select(book => book);

            int count = query.Count();
            Assert.AreEqual(1, count);
        }


        [Test]
        public void TestNotEqualExpression()
        {
            var query = from book in bookContext
                        where book.Author != "Paolo Pialorsi"
                        select book;

            Book foundBook = query.Single();

            Assert.IsNotNull(foundBook);
            Assert.AreEqual(foundBook.Author, books[1].Author);
        }

        [Test]
        public void TestMultipleItemsPerWhere()
        {
            this.Add();

            var query = from book in bookContext
                        where book.Id > books[0].Id && book.Id < books[2].Id
                        select book;

            // should be successful.
            Book b = query.Single();
            // check if the proper item is get.
            Assert.IsTrue(b.Id == books[1].Id);
        }

        [Test]
        public void TestUpdateItem()
        {
            var query = from book0 in bookContext
                        where book0.Id == books[0].Id
                        select book0;

            Book book = query.Single();

            string oldISBN = book.ISBN;
            string newISBN = "1100001";

            book.ISBN = newISBN;

            bookContext.SubmitChanges();

            Assert.IsTrue(book.ISBN == newISBN);

            query = from book1 in bookContext
                    where book1.Id == books[0].Id
                    select book1;

            book = query.Single();

            Assert.IsTrue(book.ISBN == newISBN);
        }

        // trying a member access in order by.
        public enum BookOrder
        {
            LastUpdated,
            Title
        }

        [Test]
        public void TestSingleItemOrderBy()
        {
            this.Add();

            BookOrder order = BookOrder.LastUpdated;

            var query = (from q in bookContext
                         orderby order descending
                         select new { q.Id, q.Title, q.LastUpdated }).Take(1);

            var book = query.Single();

            Assert.AreEqual(1, query.Count());
            Assert.IsTrue(book.Title == books[2].Title && book.LastUpdated == books[2].LastUpdated);
        }

        public void Add()
        {
            Book bk = new Book();

            bk.Author = "Don Box";
            bk.Title = "WPF Essentials";
            bk.ISBN = "100-11-777";
            bk.LastUpdated = DateTime.Now;

            bookContext.Add(bk);
            bookContext.SubmitChanges();

            books.Add(bk);
        }

        [Test]
        public void TestNewlyAdded()
        {
            this.Add();
            // checking complext comparsion check.

            var query = from q in bookContext
                        where q.Author == books[2].Author
                        select q;

            Book newBook = query.Single();
            Assert.IsTrue(books[2].Author == newBook.Author);
        }

        [Test]
        public void ToList()
        {
            this.Add();

            var query = from q in bookContext
                        select new { q.Id, q.ISBN };

            IList list = query.ToList();

            Assert.IsTrue(list.Count == 3);
        }

        [Test]
        public void MultipleItemOrderBy()
        {
            this.Add();
            // There are three items , and two page , where pagelen is 2, then the following 
            // query is to get the item from page 2 and item no # 3, which is the lastest book.

            var query = from q in bookContext
                        orderby q.LastUpdated descending
                        select q;

            Book book = query.FirstOrDefault();

            Assert.IsTrue(books[2].ISBN == book.ISBN);
        }


        [Test]
        public void SearhByTitle()
        {
            var query = from book in bookContext
                        where book.Author == "Paolo Pialorsi"
                        select book;

            Assert.IsTrue(query.Count() == 1);
        }

        [Test]
        public void TakeAndSkipTest()
        {
            this.Add();
            // There are three items , and two page , where pagelen is 2, then the following 
            // query is to get the item from page 2 and item no # 3, which is the lastest book.
            // trying string order by
            var query = (from q in bookContext
                         orderby "LastUpdated" ascending
                         select q).Take(1).Skip(2);

            //0 :  1 , 2 , 1: 2 , 3,  2: 3, 4

            Book book = query.Single();

            Assert.IsTrue(books[2].ISBN == book.ISBN);
        }

        [Test]
        public void SelectNotExistData()
        {
            var q = from book in bookContext
                    where book.Id == 1
                    select book;

            Assert.AreEqual(null, q.FirstOrDefault());
        }

        [Test]
        public void MultipleOrderby()
        {
            this.Add();

            var query = from book in bookContext
                        where ((book.Author == "Mehfuz" && book.ISBN == "2") || book.Id == books[2].Id) || (book.Author == "Paolo Pialorsi" && (book.ISBN == "100-11-777" || book.ISBN == "1"))
                        orderby book.Id descending, book.Title ascending
                        select book;

            int count = query.Count();

            Assert.AreEqual(1, query.Count());

            Book b = query.Single();

            Assert.AreEqual(b.Author, "Don Box");
        }

        [Test]
        public void SelectIn()
        {
            IList<string> authorFilters = new[] { "Paolo Pialorsi", "Robert Pickering" };

            var query = from book in bookContext
                        where authorFilters.Contains(book.Author)
                        select book;

            Assert.AreEqual(query.Count(), 2);

            query = from book in bookContext
                    where new List<string> { "Paolo Pialorsi", "Robert Pickering" }.Contains(book.Author)
                    select book;

            Assert.AreEqual(query.Count(), 2);

            // sql语言优化，只有一条记录是用=
            query = from book in bookContext
                    where new List<string> { "Paolo Pialorsi" }.Contains(book.Author)
                    select book;

            Assert.AreEqual(query.Count(), 1);

            //query = from book in bookContext
            //        where new List<string>().Contains(book.Author)
            //        select book;

            //Assert.AreEqual(query.Count(), 0);
        }

        [Test]
        public void Delete()
        {
            int id = books[0].Id;
            Book book = new Book() { Id = id };
            bookContext.Add(book);
            bookContext.Remove(book);

            bookContext.SubmitChanges();

            Assert.AreEqual((from b in bookContext
                             where b.Id == id
                             select b).Count(), 0);
        }

        [Test]
        public void Transaction()
        {
            using (TransactionScope scope = new TransactionScope(ConfigurationManager.ConnectionStrings["sqlite"]))
            {
                Book obj = new Book();
                obj.Author = "123";
                bookContext.Add(obj);

                obj = new Book();
                obj.Author = "456";
                bookContext.Add(obj);

                bookContext.SubmitChanges(true);

                Library obj2 = new Library();
                obj2.Floor = "123";

                libraryContext.Add(obj2);
                libraryContext.SubmitChanges();

                scope.Complete();
            }

            Assert.AreEqual((from q in libraryContext
                             where q.Floor == "123"
                             select q).Count(), 1);
        }

        //[Test]
        //public void CallMethod ( )
        //{
        //    Add ( );

        //    var query = from book in bookContext
        //                where book.Id  
        //                select book;

        //    query.FirstOrDefault ( );

        //    Assert.AreEqual ( books[ 0 ].Id, query.FirstOrDefault ( ).Id );

        //    Assert.AreNotEqual ( books[ 0 ], query.FirstOrDefault ( ) );
        //}

        //[Test]
        //public void MultiCallInSameContext()
        //{
        //    Book bk = new Book();

        //    var query =  from book in bookContext
        //                 where book.Author == "Robert Pickering"
        //                 select book;

        //    bk = query.SingleOrDefault();

        //    query = from book in bookContext
        //            where book.Author == bk.Author
        //            select book;

        //    bk.Author = "cofd";
        //    bookContext.Add( bk );
        //    bookContext.SubmitChanges();

        //    query = from book in bookContext
        //            where book.Author == bk.Author
        //            select book;

        //    Assert.AreEqual( true, query.SingleOrDefault().Author == "cofd" );
        //}

        [TearDown]
        public void Destroy()
        {
            books.Clear();
            shelves.Clear();
            DeleteAll();
        }
        private void DeleteAll()
        {
            var query = from q in bookContext
                        select q;

            foreach (var book in query)
                bookContext.Remove(book);

            bookContext.SubmitChanges();

            var shelveQuery = from q in shelveContext
                              select q;

            foreach (var shelve in shelveQuery)
                shelveContext.Remove(shelve);

            shelveContext.SubmitChanges();

            var libQuery = from q in libraryContext
                           select q;

            foreach (var library in libQuery)
                libraryContext.Remove(library);

            libraryContext.SubmitChanges();
        }

        #region IDisposable Members

        public void Dispose()
        {
            bookContext = null;
        }

        #endregion

        private SqlQuery<Book> bookContext;
        private readonly SqlQuery<Library> libraryContext;
        private readonly SqlQuery<Shelve> shelveContext;
        readonly IList<Book> books = new List<Book>();
        readonly IList<Shelve> shelves = new List<Shelve>();
    }

}
