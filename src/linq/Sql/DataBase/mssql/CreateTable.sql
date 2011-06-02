IF Not EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[#ENTITY#]') AND type in (N'U'))
CREATE TABLE [#ENTITY#]
(
	#PARAMS#
)ON [PRIMARY];