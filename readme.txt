KISS ORM

����linq��һ��������ORM.

Լ����
1������������������/ɾ/��
2��ģ���಻�ܼ̳�

�汾��ʷ��

DeleteById������id����0ʱ�ᴴ��һ����¼

v1.8
���Ӷ�sql server 2000��֧��
�޸�������string����ѯʱ��bug

v1.7
�Ż�ȡ�����ַ����Ĵ��룬���ӻ��棬������Ĭ�������ַ���
ʵ��IQuery��ȡ��DataTable�Ľӿ�
Repository����<add name="PerRequestLifestyle" type="Castle.MicroKernel.Lifestyle.PerWebRequestLifestyleModule, Castle.MicroKernel" /> httpmodule������

v1.6
�޸����ڲ��������TSqlFormatProvider��bug��ÿ��ʵ�����µ�ʵ������Ҫ���£�
�޸��˷�ҳ�б����ݻ����bug
�޸���Repository<T,t>Get(t id)��bug��Equals��������Ӵ���bucketImpl.Items[memberName].Values.Add(new BucketItem.QueryCondition(val, RelationType.Equal));
���ڸ���QueryCondition��Field�ֶ�������
Count��������Field�ֶ�ֵ��ֱ����Count(*)	

v1.5
�ع��˴��룬�Ƴ���DatabaseContext��Ĳ�ѯ�¼����Ƶ���Kiss.QueryObject)
TSqlFormatProvider��������ͬ����
QueryExtension��������ͬ����

v1.4
�޸�DDL���ò��������ݿ������ַ������������ݿ�����ƣ�������ģ������
�޸�RepositoryInitializer�����ã����������£�
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
�Ƴ���DDL��plugin��ǩ����Ϊstatic����
�������ݿ������ַ��������ù���
�Ƴ�DataBaseInitializer��Core���̵�RepositoryInitializer
Repository��pagesize=-1��querycondition����Ϊpagesize=20
contains�Ż���ֻ��һ����¼����=

v1.2
linq��ѯ֧��Contains��StartsWith��EndsWith����
linq ddl ֧��ͨ���������Ҫ�Զ�ά��������ơ�������ddl_types
CreatedEventArgs����ModelType����
����֧��.net framework 2.0

v1.1
�޸��˶���OriginalName��ǩ�ֶ������bug
�޸��˶Է�ҳCount�Ĵ���֧���Զ����count��field��

v1.0
ddl֧�ֻ�ȡ���ֶε�������sql server 2005/2008��
sqlite��ҳ���ؼ�¼��һ��
�Զ�������ʱ���漰������
�޸��˱�����������int���Ͷ����һ��bug
�½�����ʱ����������������ֵ,AutoIncreament = false
�޸��˳���һ���쳣�󣬺�����ѯ��ʧ�ܵ�bug
IN��ѯ����Ϊ��ʱ���׳��쳣
���䷽���޸�GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)�� ʵ���಻��֧�ּ̳�
�޸�Repository<T, t>���ͽӿڣ���.net framework2.0�·�����׳�ReflectionTypeLoadException�쳣
ִ��Repository�ķ�linq����ʱ�Զ�������