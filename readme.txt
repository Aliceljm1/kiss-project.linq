KISS ORM

����linq��һ��������ORM.

Լ����
1������������������/ɾ/��
2��ģ���಻�ܼ̳�

�汾��ʷ��
v2.6.1
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

v2.6.2
�޸��˶���OriginalName��ǩ�ֶ������bug
�޸��˶Է�ҳCount�Ĵ���֧���Զ����count��field��

v2.6.3
linq��ѯ֧��Contains��StartsWith��EndsWith����
linq ddl ֧��ͨ���������Ҫ�Զ�ά��������ơ�������ddl_types
CreatedEventArgs����ModelType����
����֧��.net framework 2.0

v2.6.4
�Ƴ���DDL��plugin��ǩ����Ϊstatic����
�������ݿ������ַ��������ù���
�Ƴ�DataBaseInitializer��Core���̵�RepositoryInitializer
Repository��pagesize=-1��querycondition����Ϊpagesize=20
contains�Ż���ֻ��һ����¼����=

v2.6.5
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
todo��
Repository����<add name="PerRequestLifestyle" type="Castle.MicroKernel.Lifestyle.PerWebRequestLifestyleModule, Castle.MicroKernel" /> httpmodule������
