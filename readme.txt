KISS ORM

基于linq的一个轻量级ORM.

约定：
1，必须有主键才能增/删/改
2，模型类不能继承

版本历史：

DeleteById方法在id传入0时会创建一条记录

v1.8
增加对sql server 2000的支持
修复主键是string，查询时的bug

v1.7
优化取连接字符串的代码，增加缓存，并保存默认连接字符串
实现IQuery获取的DataTable的接口
Repository脱离<add name="PerRequestLifestyle" type="Castle.MicroKernel.Lifestyle.PerWebRequestLifestyleModule, Castle.MicroKernel" /> httpmodule的依赖

v1.6
修复了在并发情况下TSqlFormatProvider的bug，每次实例化新的实例（重要更新）
修复了分页列表数据缓存的bug
修复了Repository<T,t>Get(t id)的bug，Equals方法中添加代码bucketImpl.Items[memberName].Values.Add(new BucketItem.QueryCondition(val, RelationType.Equal));
不在根据QueryCondition的Field字段填充对象
Count函数忽略Field字段值，直接用Count(*)	

v1.5
重构了代码，移除了DatabaseContext类的查询事件（移到了Kiss.QueryObject)
TSqlFormatProvider类增加了同步锁
QueryExtension类增加了同步锁

v1.4
修改DDL配置参数和数据库连接字符串，基于数据库表名称，而不是模型名称
修改RepositoryInitializer的配置，新配置如下：
	<plugin name="RepositoryInitializer" type1="Kiss.Linq.Sql.Repository`1,Kiss.Linq" type2="Kiss.Linq.Sql.Repository`2,Kiss.Linq" auto_tables="">
		<providers>
		<add name="System.Data.SqlClient" type="Kiss.Linq.Sql.DataBase.SqlDataProvider,Kiss.Linq" />
		<add name="System.Data.SQLite" type="Kiss.Linq.Sql.DataBase.SqliteDataProvider,Kiss.Linq" />
		</providers>
		<conns default="cms">
		<add conn="kiss" table="g*"/>
		</conns>
	</plugin>

v1.3
移除了DDL的plugin标签，改为static方法
增加数据库连接字符串的配置功能
移除DataBaseInitializer到Core工程的RepositoryInitializer
Repository将pagesize=-1的querycondition设置为pagesize=20
contains优化，只有一条记录是用=

v1.2
linq查询支持Contains，StartsWith，EndsWith方法
linq ddl 支持通配符配置需要自动维护表的名称。配置项ddl_types
CreatedEventArgs增加ModelType属性
不在支持.net framework 2.0

v1.1
修复了对有OriginalName标签字段排序的bug
修改了对分页Count的处理，支持自定义的count（field）

v1.0
ddl支持获取表字段的描述（sql server 2005/2008）
sqlite分页返回记录少一条
自动创建表时不涉及其他表
修复了保存主键不是int类型对象的一个bug
新建对象时，可以先设置主键值,AutoIncreament = false
修改了出现一次异常后，后续查询都失败的bug
IN查询集合为空时，抛出异常
反射方法修改GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)， 实体类不再支持继承
修改Repository<T, t>泛型接口，在.net framework2.0下反射会抛出ReflectionTypeLoadException异常
执行Repository的非linq方法时自动创建表