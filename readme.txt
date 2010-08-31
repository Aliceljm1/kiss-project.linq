KISS ORM

基于linq的一个轻量级ORM.

约定：
1，必须有主键才能增/删/改
2，模型类不能继承

版本历史：
v2.6.1
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

v2.6.2
修复了对有OriginalName标签字段排序的bug
修改了对分页Count的处理，支持自定义的count（field）

v2.6.3
linq查询支持Contains，StartsWith，EndsWith方法
linq ddl 支持通配符配置需要自动维护表的名称。配置项ddl_types
CreatedEventArgs增加ModelType属性
不在支持.net framework 2.0

v2.6.4
移除了DDL的plugin标签，改为static方法
增加数据库连接字符串的配置功能
移除DataBaseInitializer到Core工程的RepositoryInitializer
Repository将pagesize=-1的querycondition设置为pagesize=20
contains优化，只有一条记录是用=

v2.6.5
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
todo：
Repository脱离<add name="PerRequestLifestyle" type="Castle.MicroKernel.Lifestyle.PerWebRequestLifestyleModule, Castle.MicroKernel" /> httpmodule的依赖
