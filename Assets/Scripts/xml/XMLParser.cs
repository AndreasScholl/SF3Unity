using UnityEngine;
using System.Collections;
using System.Text;

public static class XMLParser : object {
    private const char LT = '<';
    private const char GT = '>';
    private const char SQR = ']';
    //private const char SQL = '[';
    private const char DASH = '-';
    private const char SPACE = ' ';
    private const char QUOTE = '"';
    private const char SLASH = '/';
    private const char QMARK = '?';
    private const char EQUALS = '=';
    private const char NEWLINE = '\n';
    private const char EXCLAMATION = '!';
    
    
    public static XMLNode Parse(string content) 
	{	
		// Set up variables
        bool inMetaTag = false;
        bool inComment = false;
        bool inCDATA = false;
        bool inElement = false;
        bool collectNodeName = false;
        bool collectAttributeName = false;
        bool collectAttributeValue = false;
        bool quoted = false;
        StringBuilder attName = new StringBuilder("", 1024);
        StringBuilder attValue = new StringBuilder("", 1024);
        StringBuilder nodeName = new StringBuilder("", 1024);
        StringBuilder textValue = new StringBuilder("", 1024);
        //string nodeContents = "";
		
        XMLNodeList parents = new XMLNodeList();

        XMLNode rootNode = new XMLNode();
        rootNode["_text"] = "";

        XMLNode currentNode = rootNode;
        
        // Process Input
        for(int i=0; i < content.Length; i++)
        {
            // Store current and nearby characters
            char c, cn, cnn, cp;
            cn = cnn =  cp = '\x00';
            c = content[i];
            if((i+1)<content.Length) cn=content[i+1]; 
            if((i+2)<content.Length) cnn=content[i+2]; 
            if(i>0)cp=content[i-1];

            
            // Process Meta Tag information
            if(inMetaTag) {
                if(c==QMARK && cn==GT) { // End of Meta Tag
                    inMetaTag = false;
                    i++;
                }
                continue;
            } else {
                if(!quoted && c==LT && cn==QMARK) { // Start of Meta Tag
                    inMetaTag = true;
                    continue;    
                }    
            }
            

            // Process Comment information
            if(inComment) {
                if(cp==DASH && c==DASH && cn==GT) { // End of comment
                    inComment = false;
                    i++;
                }
                continue;    
            } else {
                if(!quoted && c==LT && cn==EXCLAMATION) { // Start of comment or CDATA 
                    if(content.Length > (i+9) && content.Substring(i,9)=="<![CDATA[") {
                        inCDATA=true;
                        i+=8;
                    } else {                    
                        inComment=true;
                    }
                    continue;    
                }
            }
            

            // Process CDATA information
            if(inCDATA){
                if(c==SQR && cn==SQR && cnn==GT){
                    inCDATA=false;
                    i+=2;
                    continue;
                }
                textValue.Append(c);
                continue;    
            }
            

            // Process Elements
            if(inElement){
                
                if (collectNodeName){
                 	// collect the node name
					if(c==SPACE)
					{
                        collectNodeName=false;
                    }
					else if(c==GT)
					{
                        collectNodeName=false;
                        inElement=false;
                    }
            
                    
                    if(!collectNodeName && nodeName.Length>0){
                        if(nodeName[0]==SLASH){
                            // close tag
                            if(textValue.Length>0){
                                currentNode["_text"] += textValue.ToString();
                            }
                    
                            textValue.Length = 0;
                            nodeName.Length = 0;
                            currentNode = parents.Pop();
                        }else
						{
                            if(textValue.Length > 0){
                                currentNode["_text"] += textValue.ToString();
                            }
                            textValue.Length = 0;    
                            XMLNode newNode = new XMLNode();
							string nodeNameString = nodeName.ToString();
                            newNode["_text"] = "";
                            newNode["_name"] = nodeNameString;
							
							if(!currentNode.ContainsKey(nodeNameString)){
                                currentNode[nodeNameString] = new XMLNodeList();    
                            }
                            XMLNodeList a = currentNode[nodeNameString] as XMLNodeList;
                            a.Push(newNode);    
                            parents.Push(currentNode);
                            currentNode=newNode;
                            
                            nodeName.Length = 0;
                        }
                    }
					else
					{
                        nodeName.Append(c);    
                    }
                }
				else
				{
					//
                    // get attributes
					//
                    if(!quoted && c==SLASH && cn==GT){
                        inElement=false;
                        collectAttributeName=false;
                        collectAttributeValue=false;    
                        
						if (attName.Length > 0) 
						{
							string attNameString = attName.ToString();
							
                            if (attValue.Length > 0)
                            {
                                currentNode["@"+attNameString] = attValue.ToString();
                            }else{
                                currentNode["@"+attNameString] = true; 
                            }
                        }
                        
                        i++;
                        currentNode=parents.Pop();
                        attName.Length = 0;
                        attValue.Length = 0;
                    }
                    else if(!quoted && c==GT){
                        inElement=false;
                        collectAttributeName=false;
                        collectAttributeValue=false;    
                        if(attName.Length > 0)
						{
                            currentNode["@"+attName.ToString()] = attValue.ToString();
                        }
                        
                        attName.Length = 0;
                        attValue.Length = 0;
                    }else{
                        if(collectAttributeName)
						{
                            if(c==SPACE || c==EQUALS)
							{
                                collectAttributeName=false;    
                                collectAttributeValue=true;
                            }
							else
							{
                                attName.Append(c);
                            }
                        }else if(collectAttributeValue){
                            if(c==QUOTE){
                                if(quoted){
                                    collectAttributeValue=false;
                                    currentNode["@"+attName.ToString()] = attValue.ToString();                                
                                    attValue.Length = 0;
                                    attName.Length = 0;
                                    quoted=false;
                                }
								else
								{
                                    quoted=true;    
                                }
                            }else{
                                if (quoted) 
								{
                                    attValue.Append(c);    
                                }
								else
								{
                                    if (c==SPACE)
									{
                                        collectAttributeValue=false;    
                                        currentNode["@"+attName.ToString()] = attValue.ToString();
	                                    attValue.Length = 0;
    	                                attName.Length = 0;
                                    }
                                }
                            }
                        }
						else if (c==SPACE)
						{
                        }
						else
						{
                            collectAttributeName=true;
                            attName.Length = 0;
							attName.Append(c);
                            attValue.Length = 0;
                            quoted = false;        
                        }    
                    }
                }
                
            }
			else
			{
                if (c==LT) { // Start of new element
                    inElement=true;
                    collectNodeName=true;    
                }else{
                    textValue.Append(c); // text between elements
                }    
                
            }
            
                
        }
		
        return rootNode;
    }
}
