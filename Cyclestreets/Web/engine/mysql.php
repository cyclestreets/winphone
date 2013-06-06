<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/mysql.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/
 
require_once("debug.php");
    
// Global data so we dont need to keep re-selecting the database unless we need to.
// It's expensive to do this every query
$last_conn = array( "host" => "",
                    "user" => "",
                    "pass" => "",
                    "db" => "");
class mysql {

    var $sql_database	= "";
	var $sql_user		= "";
    var $sql_pass		= "";
	var $sql_host		= "localhost";
	var $sql_tbl_prefix = "dl_";

	var $query_id      	= "";
    var $db 			= "";
    var $query_count   	= 0;
	var $query_string 	= array();
	
	var $stayAlive 		= 0;		// Beegees tribute
	var $failed			= 0;

	function mysql( $data )
	{
        global $CONFIG;

		$this->sql_host = $data["sqlhost"];
		$this->sql_user = $data["sqlusername"];
		$this->sql_pass = $data["sqlpassword"];
		$this->sql_database = $data["sqldatabase"];
		$this->sql_tbl_prefix = $data["sql_tbl_prefix"];

        if ( $CONFIG['mysqlPersist'] )
        {
            $this->db = mysql_pconnect( $this->sql_host, $this->sql_user ,
                                   $this->sql_pass );
        }
        else
        {
		    $this->db = mysql_connect( $this->sql_host, $this->sql_user ,
								   $this->sql_pass );
        }
	}

	function selectDB($doError=true)
	{
        global $last_conn;
        if ( $last_conn['db'] != $this->sql_database ||
             $last_conn['host'] != $this->sql_host ||
             $last_conn['user'] != $this->sql_user ||
             $last_conn['pass'] != $this->sql_pass )
        {
            $last_conn['db'] = $this->sql_database;
            $last_conn['host'] = $this->sql_host;
            $last_conn['user'] = $this->sql_user;
            $last_conn['pass'] = $this->sql_pass;
            
		    if ( !@mysql_select_db( $this->sql_database, $this->db ) )
            {
                if ( $doError )
                    $this->doError ("ERROR: Cannot find database ".$this->sql_database);
                return false;
            }
            else
            {
                return true;
            }
        }
        else
            return true;
	}
	
	function query($the_query) 
	{
		$this->selectDB();
		if ($this->sql_tbl_prefix != "dl_")
		   $the_query = preg_replace("/dl_(\S+?)([\s\.,]|$)/", $this->sql_tbl_prefix."\\1\\2", $the_query);
    	
		$this->query_id = mysql_query($the_query, $this->db);
        if (! $this->query_id )
            $this->doError ("mySQL query error: $the_query");    
		
		$this->query_count++;
  		$this->query_string[] = $the_query;
        return $this->query_id;
    }
	
    function createInsertQuery($the_query, $table)
    {  
        $fieldArray = "";
        $valueArray = "";
        // Should create a insert statement from a array
        foreach( $the_query as $field => $value )
        {
            $fieldArray .= "`$field`, ";
            $valueArray .= "'$value', ";
        }
        // trim the last comma
        $fieldArray = substr($fieldArray,0, -2);
        $valueArray = substr($valueArray,0, -2);
        $sql = "INSERT INTO `$table` ( ".$fieldArray." ) VALUES ( ".$valueArray." )";
        return $sql;
    }
    
	function insert($the_query, $table)
	{
        $sql = $this->createInsertQuery($the_query, $table);
		
		$this->query_id = $this->query($sql);
  
        return $this->query_id;
	}
	
	function update($the_query, $table, $where)
	{
		$fieldArray = "";

		// Should create a insert statement from a array
		foreach( $the_query as $field => $value )
		{
			$fieldArray .= "`$field`=";
			if ( stristr( $value, "$field+" ) || stristr( $value, "$field-" ))
				$fieldArray .= "$value, ";
			else
				$fieldArray .= "'$value', ";
		}
		// trim the last comma
		$fieldArray = substr($fieldArray,0, -2);
		$sql = "UPDATE `$table` SET $fieldArray WHERE $where";
		
		$this->query_id = $this->query($sql);
  
        return $this->query_id;
	}
	
	function fetch_row($query_id = "", $type=MYSQL_ASSOC)
	{
        /*if ( is_bool($query_id) === true ||  empty($query_id) )
        {
            echo "Here";
            require_once(ROOT_PATH."/engine/debug.php");
            getTrace();
        }   */
		$this->selectDB();
    	if ($query_id == "")
    		$query_id = $this->query_id;
        $record_row = mysql_fetch_array($query_id, $type);
        return $record_row;
    }
    
    function fetch_object($class=null, $query_id = "")
    {
        $this->selectDB();
        if ($query_id == "")
            $query_id = $this->query_id;
        $record_row = mysql_fetch_object($query_id, $class);
        return $record_row;
    }
	
	function field_exists($field, $table) 
	{
		$this->stayAlive = 1;
		
		$this->query("SELECT COUNT($field) as count FROM $table");
		
		$return = true;
		
		if ( $this->failed )
			$return = false;
		
		$this->failed = 0;
		
		return $return;
	}
	
	function num_rows($query_id = "") 
	{
		$this->selectDB();
    	if ($query_id == "")
    		$query_id = $this->query_id;
        $num_rows = mysql_num_rows($this->query_id);
		mysql_error();
		return $num_rows;
    }
	
	function affected_rows() 
	{
		$this->selectDB();
    	return mysql_affected_rows($this->db);
    }
	
	function insert_id()
	{
		$this->selectDB();
        return mysql_insert_id($this->db);
    }  
	
	function close_db() 
	{ 
		if ( $this->db && $this->selectDB(false) )
        {
            @mysql_close($this->db);
            $this->db = 0;
        }   
    }

	function doError($the_error)
	{
		global $OUTPUT, $CONFIG, $IN, $std;
		
		$this->failed = 1;
		if ( $this->stayAlive) // Dont crash with an error, just return with blisful ignorance
			return;

    	$the_error .= "\n\nmySQL error: ".mysql_error($this->db)."\n";
    	$the_error .= "Date: ".date("l dS of F Y h:i:s A");

        if ( $std )
            $std->addErrorLog(addslashes($the_error));
        
		ob_start();
		print_r($IN);
		$inarray = ob_get_contents();
		ob_end_clean();
        if ( true )
        {
            ob_start();
    		getTrace(1);
    		$stack = ob_get_contents();
    		ob_end_clean();
        }
    	$out = "Critical Error
        A database error has occoured.
    	Error Returned $the_error";
        $out .= "Stack:
        {$stack}
        
        POST GET:
        {$inarray}";
		
		mail("realworld666@gmail.com","MySQL Error",$out);
		
        echo($out);
		die("");
    }
}
?>